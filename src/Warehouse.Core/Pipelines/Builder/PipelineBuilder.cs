using Microsoft.Extensions.Logging;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Registry;
using Warehouse.Core.Pipelines.Steps;
using Warehouse.Core.Pipelines.Trading;

namespace Warehouse.Core.Pipelines.Builder;

/// <summary>
///     Builds pipeline step instances from database configuration.
/// </summary>
public class PipelineBuilder(IStepRegistry stepRegistry, ILogger<PipelineBuilder> logger) : IPipelineBuilder
{
    /// <inheritdoc/>
    public IReadOnlyList<IPipelineStep<TradingContext>> BuildSteps(Pipeline pipeline, IServiceProvider services)
    {
        var steps = new List<IPipelineStep<TradingContext>>();

        foreach (PipelineStep stepConfig in pipeline.Steps.Where(s => s.IsEnabled).OrderBy(s => s.Order))
        {
            try
            {
                if (string.IsNullOrEmpty(stepConfig.StepTypeKey))
                {
                    logger.LogWarning("Step {StepId} in pipeline {PipelineId} has no StepTypeKey, skipping", stepConfig.Id, pipeline.Id);
                    continue;
                }

                IStepDefinition? definition = stepRegistry.GetDefinition(stepConfig.StepTypeKey);
                if (definition is null)
                {
                    logger.LogWarning(
                        "Step definition not found for key '{StepTypeKey}' in pipeline {PipelineId}",
                        stepConfig.StepTypeKey,
                        pipeline.Id);
                    continue;
                }

                var parameterBag = new ParameterBag(stepConfig.Parameters);
                IPipelineStep<TradingContext> step = definition.CreateInstance(services, parameterBag);

                steps.Add(step);
                logger.LogDebug("Created step instance: {StepName} for pipeline {PipelineId}", definition.Name, pipeline.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to create step instance for {StepTypeKey} in pipeline {PipelineId}",
                    stepConfig.StepTypeKey,
                    pipeline.Id);
            }
        }

        return steps.AsReadOnly();
    }

    /// <inheritdoc/>
    public ValidationResult ValidatePipeline(Pipeline pipeline)
    {
        var errors = new List<ValidationError>();

        if (!pipeline.Steps.Any(s => s.IsEnabled))
        {
            errors.Add(new ValidationError("steps", "Pipeline must have at least one enabled step."));
        }

        foreach (PipelineStep stepConfig in pipeline.Steps.Where(s => s.IsEnabled))
        {
            if (string.IsNullOrEmpty(stepConfig.StepTypeKey))
            {
                errors.Add(new ValidationError($"step_{stepConfig.Id}", "Step has no type key."));
                continue;
            }

            ValidationResult validationResult = stepRegistry.ValidateParameters(stepConfig.StepTypeKey, stepConfig.Parameters);
            if (!validationResult.IsValid)
            {
                foreach (ValidationError error in validationResult.Errors)
                {
                    errors.Add(new ValidationError($"step_{stepConfig.Id}.{error.Key}", error.Message));
                }
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
