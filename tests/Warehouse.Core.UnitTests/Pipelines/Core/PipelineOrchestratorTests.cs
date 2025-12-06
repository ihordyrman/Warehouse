using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Builder;
using Warehouse.Core.Pipelines.Core;
using Warehouse.Core.Pipelines.Domain;
using Warehouse.Core.Pipelines.Parameters;
using Xunit;

namespace Warehouse.Core.UnitTests.Pipelines.Core;

public class PipelineOrchestratorTests
{
    private readonly WarehouseDbContext dbContext;
    private readonly IPipelineExecutorFactory executorFactory;
    private readonly PipelineOrchestrator orchestrator;
    private readonly IPipelineBuilder pipelineBuilder;

    public PipelineOrchestratorTests()
    {
        DbContextOptions<WarehouseDbContext> options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        dbContext = new WarehouseDbContext(options, null);
        executorFactory = Substitute.For<IPipelineExecutorFactory>();
        pipelineBuilder = Substitute.For<IPipelineBuilder>();

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(pipelineBuilder);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        orchestrator = new PipelineOrchestrator(scopeFactory, NullLogger<PipelineOrchestrator>.Instance, executorFactory);
    }

    [Fact]
    public async Task SynchronizePipelinesAsync_ShouldStartEnabledPipelines()
    {
        var pipeline = new Pipeline
        {
            Id = 1,
            Enabled = true,
            Status = PipelineStatus.Running,
            Steps = []
        };

        dbContext.PipelineConfigurations.Add(pipeline);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        pipelineBuilder.ValidatePipeline(Arg.Any<Pipeline>()).Returns(new ValidationResult(true, []));

        IPipelineExecutor? executor = Substitute.For<IPipelineExecutor>();
        executorFactory.Create(Arg.Is<Pipeline>(p => p.Id == 1), Arg.Any<IServiceProvider>()).Returns(executor);

        // Act
        await orchestrator.SynchronizePipelinesAsync();

        // Assert
        executorFactory.Received(1).Create(Arg.Is<Pipeline>(p => p.Id == 1), Arg.Any<IServiceProvider>());
        await executor.Received(1).StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizePipelinesAsync_ShouldNotStartPausedPipelines()
    {
        // Arrange
        var pipeline = new Pipeline { Id = 1, Enabled = true, Status = PipelineStatus.Paused, Steps = [] };
        dbContext.PipelineConfigurations.Add(pipeline);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await orchestrator.SynchronizePipelinesAsync();

        // Assert
        executorFactory.DidNotReceive().Create(Arg.Is<Pipeline>(p => p.Id == 1), Arg.Any<IServiceProvider>());
    }

    [Fact]
    public async Task StartPipelineAsync_ShouldStartPipeline_WhenNotRunning()
    {
        // Arrange
        var pipeline = new Pipeline { Id = 1, Enabled = true, Status = PipelineStatus.Idle, Steps = [] };
        dbContext.PipelineConfigurations.Add(pipeline);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        pipelineBuilder.ValidatePipeline(Arg.Any<Pipeline>()).Returns(new ValidationResult(true, []));

        IPipelineExecutor? executor = Substitute.For<IPipelineExecutor>();
        executorFactory.Create(Arg.Is<Pipeline>(p => p.Id == 1), Arg.Any<IServiceProvider>()).Returns(executor);

        // Act
        bool result = await orchestrator.StartPipelineAsync(1);

        // Assert
        Assert.True(result);
        await executor.Received(1).StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopPipelineAsync_ShouldStopPipeline_WhenRunning()
    {
        // Arrange
        var pipeline = new Pipeline { Id = 1, Enabled = true, Status = PipelineStatus.Running, Steps = [] };
        dbContext.PipelineConfigurations.Add(pipeline);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        pipelineBuilder.ValidatePipeline(Arg.Any<Pipeline>()).Returns(new ValidationResult(true, []));

        IPipelineExecutor? executor = Substitute.For<IPipelineExecutor>();
        executorFactory.Create(Arg.Is<Pipeline>(p => p.Id == 1), Arg.Any<IServiceProvider>()).Returns(executor);
        await orchestrator.StartPipelineAsync(1);

        // Act
        bool result = await orchestrator.StopPipelineAsync(1);

        // Assert
        Assert.True(result);
        await executor.Received(1).StopAsync();
    }

    [Fact]
    public async Task StartPipelineAsync_ShouldFailValidator_WhenBuilderFails()
    {
        // Arrange
        var pipeline = new Pipeline { Id = 1, Enabled = true, Status = PipelineStatus.Idle, Steps = [] };
        dbContext.PipelineConfigurations.Add(pipeline);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        pipelineBuilder.ValidatePipeline(Arg.Any<Pipeline>())
            .Returns(new ValidationResult(false, [new ValidationError("err", "Invalid pipeline")]));

        // Act
        bool result = await orchestrator.StartPipelineAsync(1);

        // Assert
        Assert.False(result);
        executorFactory.DidNotReceive().Create(Arg.Any<Pipeline>(), Arg.Any<IServiceProvider>());
    }
}
