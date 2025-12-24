using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Old.Functional.Pipelines.Domain;
using Warehouse.Core.Old.Pipelines.Core;
using Warehouse.Core.Old.Pipelines.Parameters;
using Warehouse.Core.Old.Pipelines.Steps;

namespace Warehouse.Core.Old.Pipelines.Trading.Steps;

/// <summary>
///     Definition for the Open Position step.
/// </summary>
[Pipelines.Steps.StepDefinition("open-position")]
public class OpenPositionStepDefinition : BaseStepDefinition
{
    private const string ParamEntryPrice = "entyPrice";
    private const decimal DefaultEntryPrice = 10m;

    public override string Key => "open-position";

    public override string Name => "Open Position";

    public override string Description => "Opens a new trading position if no open position exists.";

    public override StepCategory Category => StepCategory.Signal;

    public override string Icon => "fa-dollar";

    public override ParameterSchema GetParameterSchema()
        => new ParameterSchema().AddDecimal(
            ParamEntryPrice,
            "Amount to Invest (USDT)",
            "Amount in USDT to invest when opening a new position",
            true,
            DefaultEntryPrice,
            0.1m,
            100m,
            "Order Settings");

    public override IPipelineStep<TradingContext> CreateInstance(IServiceProvider services, ParameterBag parameters)
    {
        IServiceScopeFactory scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        return new OpenPositionStep(scopeFactory)
        {
            Parameters =
            {
                ["EntryPrice"] = parameters.GetDecimal(ParamEntryPrice, DefaultEntryPrice)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        };
    }
}

/// <summary>
///     Opens a new trading position if no open position exists.
/// </summary>
public class OpenPositionStep(IServiceScopeFactory serviceScopeFactory) : IPipelineStep<TradingContext>
{
    public PipelineStepType Type => PipelineStepType.Validation;

    public string Name => "OpenPosition";

    public Dictionary<string, string> Parameters { get; } = [];

    public async Task<PipelineStepResult> ExecuteAsync(TradingContext context, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
        IDbConnection db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

        var openPosition = await db.QueryFirstOrDefaultAsync<Position?>(
            "SELECT * FROM positions WHERE pipeline_id = @PipelineId AND status = @Status",
            new { PipelineId = context.PipelineId, Status = (int)PositionStatus.Open });

        if (openPosition is not null)
        {
            context.Action = TradingAction.None;
            return PipelineStepResult.Continue("No action needed - Position already open");
        }

        decimal entryPrice = Parameters.TryGetValue("EntryPrice", out string? priceStr) &&
                             decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) ?
            price :
            throw new InvalidOperationException("Invalid EntryPrice parameter");

        context.BuyPrice = entryPrice;
        context.Action = TradingAction.Buy;

        return PipelineStepResult.Continue("No open position - setting action to Buy");
    }
}
