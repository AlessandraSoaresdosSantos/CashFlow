using CashFlow.ConsolidationWorker;
using CashFlow.Domain.Events;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CashFlow.UnitTests.Workers;

public class WorkerTests
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IConsumer<Ignore, string>> _mockConsumer;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockConsumer = new Mock<IConsumer<Ignore, string>>();

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSubscribeAndConsume_UntilCancelled()
    {
        // Arrange
        var worker = new Worker(_mockLogger.Object, _mockConsumer.Object, _mockServiceProvider.Object);
        var cts = new CancellationTokenSource();

        _mockConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(cts.Token);

        // Assert
        _mockConsumer.Verify(c => c.Subscribe("entry-events-topic"), Times.Once);
        _mockConsumer.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenValidMessage_ShouldDeserializeAndCreateScope()
    {
        // Arrange
        var worker = new Worker(_mockLogger.Object, _mockConsumer.Object, _mockServiceProvider.Object);

        var validEvent = new EntryCreatedEvent(
            Guid.NewGuid(),
            150.0m,
            (CashFlow.Domain.Enums.EntryType)1, 
            DateTime.UtcNow
        );

        var jsonMessage = JsonSerializer.Serialize(validEvent);

        // Act
        await worker.ProcessMessageAsync(jsonMessage, CancellationToken.None);

        // Assert
        _mockScopeFactory.Verify(s => s.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenInvalidMessage_ShouldNotCreateScopeAndLogWarning()
    {
        // Arrange
        var worker = new Worker(_mockLogger.Object, _mockConsumer.Object, _mockServiceProvider.Object);
        var invalidJsonMessage = "isso-nao-e-um-json";

        // Act
        await worker.ProcessMessageAsync(invalidJsonMessage, CancellationToken.None);

        // Assert
        _mockScopeFactory.Verify(s => s.CreateScope(), Times.Never);
    }
}