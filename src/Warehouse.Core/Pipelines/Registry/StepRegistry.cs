using System.Reflection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Steps;
using Warehouse.Core.Pipelines.Trading;

namespace Warehouse.Core.Pipelines.Registry;

/// <summary>
///     Implementation of step registry that discovers step definitions at startup.
/// </summary>
public class StepRegistry : IStepRegistry
{
    private readonly Dictionary<string, IStepDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<StepRegistry> logger;

    public StepRegistry(ILogger<StepRegistry> logger)
    {
        this.logger = logger;
        DiscoverDefinitions();
    }

    /// <inheritdoc/>
    public IReadOnlyList<IStepDefinition> GetAllDefinitions() => definitions.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public IStepDefinition? GetDefinition(string key) => definitions.GetValueOrDefault(key);

    /// <inheritdoc/>
    public IEnumerable<IStepDefinition> GetByCategory(StepCategory category) => definitions.Values.Where(x => x.Category == category);

    /// <inheritdoc/>
    public IPipelineStep<TradingContext> CreateInstance(string key, IServiceProvider services, ParameterBag parameters)
    {
        IStepDefinition definition = GetDefinition(key) ?? throw new InvalidOperationException($"Step definition not found: {key}");

        return definition.CreateInstance(services, parameters);
    }

    /// <inheritdoc/>
    public ValidationResult ValidateParameters(string key, IReadOnlyDictionary<string, string> parameters)
    {
        IStepDefinition? definition = GetDefinition(key);
        if (definition is null)
        {
            return new ValidationResult(false, [new ValidationError("stepTypeKey", $"Unknown step type: {key}")]);
        }

        ParameterSchema schema = definition.GetParameterSchema();
        return schema.Validate(parameters);
    }

    private void DiscoverDefinitions()
    {
        Assembly assembly = typeof(StepRegistry).Assembly;
        IEnumerable<Type> definitionTypes = assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false })
            .Where(x => x.GetCustomAttribute<StepDefinitionAttribute>() is not null)
            .Where(x => typeof(IStepDefinition).IsAssignableFrom(x));

        foreach (Type type in definitionTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is not IStepDefinition instance)
                {
                    continue;
                }

                definitions[instance.Key] = instance;
                logger.LogDebug("Registered step definition: {Key} ({Name})", instance.Key, instance.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to instantiate step definition: {Type}", type.FullName);
            }
        }

        logger.LogInformation("Discovered {Count} step definitions", definitions.Count);
    }
}
