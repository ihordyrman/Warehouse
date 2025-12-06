using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Pipelines.Builder;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Trading;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Core;

public class PipelineExecutorTests
{
    private readonly NullLogger<PipelineExecutor> logger = NullLogger<PipelineExecutor>.Instance;
    private readonly Pipeline pipeline;
    private readonly IPipelineBuilder pipelineBuilder;
    private readonly IServiceProvider serviceProvider;

    public PipelineExecutorTests()
    {
        pipelineBuilder = Substitute.For<IPipelineBuilder>();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PipelineExecutor>>(logger);
        services.AddSingleton(pipelineBuilder);

        serviceProvider = services.BuildServiceProvider();

        pipeline = new Pipeline
        {
            Id = 1,
            Steps = [],
            MarketType = MarketType.Okx,
            ExecutionInterval = TimeSpan.FromMilliseconds(50),
            Enabled = true
        };
    }

    [Fact]
    public async Task StartAsync_ShouldExecuteSteps_WhenStarted()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        pipelineBuilder.BuildSteps(Arg.Any<Pipeline>(), Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                tcs.TrySetResult(true);
                return new List<IPipelineStep<TradingContext>>();
            });

        var executor = new PipelineExecutor(serviceProvider, pipeline);

        // Act
        await executor.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executor.IsRunning);

        Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
        Assert.True(completedTask == tcs.Task, "BuildSteps was not called within timeout");

        await executor.StopAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldExecuteStepsInOrder()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();

        IPipelineStep<TradingContext>? step1 = Substitute.For<IPipelineStep<TradingContext>>();
        step1.ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>()).Returns(PipelineStepResult.Continue("Step 1 done"));

        IPipelineStep<TradingContext>? step2 = Substitute.For<IPipelineStep<TradingContext>>();
        step2.ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                tcs.TrySetResult(true); // Signal completion at step 2
                return PipelineStepResult.Continue("Step 2 done");
            });

        pipelineBuilder.BuildSteps(Arg.Any<Pipeline>(), Arg.Any<IServiceProvider>()).Returns([step1, step2]);

        var executor = new PipelineExecutor(serviceProvider, pipeline);

        // Act
        await executor.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executor.IsRunning);

        Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
        Assert.True(completedTask == tcs.Task, "Steps were not executed in time");

        await executor.StopAsync();

        Received.InOrder(async () =>
        {
            await step1.ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>());
            await step2.ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task StartAsync_ShouldStopChain_WhenStepReturnsStop()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        IPipelineStep<TradingContext>? step1 = Substitute.For<IPipelineStep<TradingContext>>();
        step1.ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult(true);
                return PipelineStepResult.Stop("Stopping");
            });

        IPipelineStep<TradingContext>? step2 = Substitute.For<IPipelineStep<TradingContext>>();

        pipelineBuilder.BuildSteps(Arg.Any<Pipeline>(), Arg.Any<IServiceProvider>()).Returns([step1, step2]);

        var executor = new PipelineExecutor(serviceProvider, pipeline);

        // Act
        await executor.StartAsync(TestContext.Current.CancellationToken);
        Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
        Assert.True(completedTask == tcs.Task, "Step 1 was not executed in time");

        await executor.StopAsync();

        // Assert
        await step1.Received(1).ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>());
        await step2.DidNotReceive().ExecuteAsync(Arg.Any<TradingContext>(), Arg.Any<CancellationToken>());
    }
}
