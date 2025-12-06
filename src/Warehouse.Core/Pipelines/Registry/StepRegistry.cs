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
    private readonly Dictionary<string, IStepDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<StepRegistry> _logger;

    public StepRegistry(ILogger<StepRegistry> logger)
    {
        _logger = logger;
        DiscoverDefinitions();
    }

    /// <inheritdoc/>
    public IReadOnlyList<IStepDefinition> GetAllDefinitions() => _definitions.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public IStepDefinition? GetDefinition(string key) => _definitions.TryGetValue(key, out IStepDefinition? definition) ? definition : null;

    /// <inheritdoc/>
    public IEnumerable<IStepDefinition> GetByCategory(StepCategory category) => _definitions.Values.Where(d => d.Category == category);

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
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<StepDefinitionAttribute>() is not null)
            .Where(t => typeof(IStepDefinition).IsAssignableFrom(t));

        foreach (Type type in definitionTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(type) as IStepDefinition;
                if (instance is not null)
                {
                    _definitions[instance.Key] = instance;
                    _logger.LogDebug("Registered step definition: {Key} ({Name})", instance.Key, instance.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate step definition: {Type}", type.FullName);
            }
        }

        _logger.LogInformation("Discovered {Count} step definitions", _definitions.Count);
    }
}
