using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Warehouse.Core.Pipelines.Builder;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Parameters;
using Warehouse.Core.Pipelines.Registry;
using Warehouse.Core.Pipelines.Steps;
using Warehouse.Core.Pipelines.Trading;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Builder;

public class PipelineBuilderTests
{
    private readonly PipelineBuilder builder;
    private readonly IStepRegistry registry;
    private readonly IServiceProvider serviceProvider;

    public PipelineBuilderTests()
    {
        registry = Substitute.For<IStepRegistry>();

        var services = new ServiceCollection();

        builder = new PipelineBuilder(registry, NullLogger<PipelineBuilder>.Instance);
        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void BuildSteps_ShouldCreateEnabledStepsInOrder()
    {
        // Arrange
        IStepDefinition? stepDef1 = Substitute.For<IStepDefinition>();
        stepDef1.CreateInstance(Arg.Any<IServiceProvider>(), Arg.Any<ParameterBag>())
            .Returns(Substitute.For<IPipelineStep<TradingContext>>());

        IStepDefinition? stepDef2 = Substitute.For<IStepDefinition>();
        stepDef2.CreateInstance(Arg.Any<IServiceProvider>(), Arg.Any<ParameterBag>())
            .Returns(Substitute.For<IPipelineStep<TradingContext>>());

        registry.GetDefinition("step-1").Returns(stepDef1);
        registry.GetDefinition("step-2").Returns(stepDef2);

        var pipeline = new Pipeline
        {
            Id = 1,
            Steps = new List<PipelineStep>
            {
                new() { Id = 1, StepTypeKey = "step-2", Order = 2, IsEnabled = true },
                new() { Id = 2, StepTypeKey = "step-1", Order = 1, IsEnabled = true },
                new() { Id = 3, StepTypeKey = "step-1", Order = 3, IsEnabled = false }
            }
        };

        // Act
        IReadOnlyList<IPipelineStep<TradingContext>> steps = builder.BuildSteps(pipeline, serviceProvider);

        // Assert
        Assert.Equal(2, steps.Count);
        stepDef1.Received(1).CreateInstance(Arg.Any<IServiceProvider>(), Arg.Any<ParameterBag>());
        stepDef2.Received(1).CreateInstance(Arg.Any<IServiceProvider>(), Arg.Any<ParameterBag>());
    }

    [Fact]
    public void BuildSteps_ShouldSkipUnknownStepTypes()
    {
        // Arrange
        registry.GetDefinition("unknown-step").Returns((IStepDefinition?)null);

        var pipeline = new Pipeline
        {
            Id = 1,
            Steps = [new PipelineStep { Id = 1, StepTypeKey = "unknown-step", Order = 1, IsEnabled = true }]
        };

        // Act
        IReadOnlyList<IPipelineStep<TradingContext>> steps = builder.BuildSteps(pipeline, serviceProvider);

        // Assert
        Assert.Empty(steps);
    }

    [Fact]
    public void ValidatePipeline_ShouldFail_WhenNoEnabledSteps()
    {
        // Arrange
        var pipeline = new Pipeline
        {
            Steps = [new PipelineStep { IsEnabled = false }]
        };

        // Act
        ValidationResult result = builder.ValidatePipeline(pipeline);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == "steps");
    }

    [Fact]
    public void ValidatePipeline_ShouldFail_WhenStepHasNoTypeKey()
    {
        // Arrange
        var pipeline = new Pipeline
        {
            Steps = [new PipelineStep { Id = 1, IsEnabled = true, StepTypeKey = "" }]
        };

        // Act
        ValidationResult result = builder.ValidatePipeline(pipeline);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == "step_1");
    }

    [Fact]
    public void ValidatePipeline_ShouldDelegateParameterValidationToRegistry()
    {
        // Arrange
        IStepDefinition? stepDef = Substitute.For<IStepDefinition>();
        registry.GetDefinition("test-step").Returns(stepDef);

        var stepParams = new Dictionary<string, string> { ["param1"] = "bad" };

        registry.ValidateParameters("test-step", Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(new ValidationResult(false, [new ValidationError("param1", "Invalid value")]));

        var pipeline = new Pipeline
        {
            Steps =
            [
                new PipelineStep
                {
                    Id = 10,
                    StepTypeKey = "test-step",
                    IsEnabled = true,
                    Parameters = stepParams
                }
            ]
        };

        // Act
        ValidationResult result = builder.ValidatePipeline(pipeline);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key == "step_10.param1" && e.Message == "Invalid value");
    }
}
