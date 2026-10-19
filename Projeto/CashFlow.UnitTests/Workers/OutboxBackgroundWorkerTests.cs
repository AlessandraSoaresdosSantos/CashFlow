using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.EntryAPI.Workers;
using CashFlow.Infrastructure.Data.Relational;

namespace CashFlow.UnitTests.Workers;

public class OutboxBackgroundWorkerTests
{
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<ILogger<OutboxBackgroundWorker>> _mockLogger;

    public OutboxBackgroundWorkerTests()
    {
        _mockMessageBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<OutboxBackgroundWorker>>();
    }

    private IServiceScopeFactory SetupScopeFactory(AppDbContext dbContext)
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(dbContext);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IMessageBus))).Returns(_mockMessageBus.Object);

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

        return mockScopeFactory.Object;
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenPendingMessagesExist_ShouldPublishAndUpdateProcessedAt()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString(); 
        using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);

        var pendingMessage = new OutboxMessage { Id = Guid.NewGuid(), Topic = "entry-events", Payload = "{ 'mock': true }" };
        dbContext.OutboxMessages.Add(pendingMessage);
        await dbContext.SaveChangesAsync();

        var scopeFactory = SetupScopeFactory(dbContext);
        var worker = new TestableOutboxBackgroundWorker(scopeFactory, _mockLogger.Object);

        // Act
        await worker.InvokeProcessAsync(CancellationToken.None);

        _mockMessageBus.Verify(m => m.PublishAsync("entry-events", "{ 'mock': true }"), Times.Once);

        
        var updatedMessage = await dbContext.OutboxMessages.FindAsync(pendingMessage.Id);
        Assert.NotNull(updatedMessage);
        Assert.NotNull(updatedMessage.ProcessedAt); 
        Assert.Null(updatedMessage.Error);         
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenKafkaThrowsException_ShouldRecordErrorAndKeepProcessedAtNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);

        var pendingMessage = new OutboxMessage { Id = Guid.NewGuid(), Topic = "entry-events", Payload = "{ 'mock': true }" };
        dbContext.OutboxMessages.Add(pendingMessage);
        await dbContext.SaveChangesAsync();

        // Simulamos uma queda no Kafka (ex: ProduceException)
        _mockMessageBus
            .Setup(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Kafka timeout / broker indisponível"));

        var scopeFactory = SetupScopeFactory(dbContext);
        var worker = new TestableOutboxBackgroundWorker(scopeFactory, _mockLogger.Object);

        // Act
        await worker.InvokeProcessAsync(CancellationToken.None);

       
        _mockMessageBus.Verify(m => m.PublishAsync("entry-events", "{ 'mock': true }"), Times.Once);

        var updatedMessage = await dbContext.OutboxMessages.FindAsync(pendingMessage.Id);
        Assert.NotNull(updatedMessage);
        Assert.Null(updatedMessage.ProcessedAt); // Continua nulo para ser tentado novamente no próximo ciclo
        Assert.Equal("Kafka timeout / broker indisponível", updatedMessage.Error); // Erro registado no SQL Server
    }

    [Fact]
    public async Task ProcessOutboxMessagesAsync_WhenNoPendingMessages_ShouldDoNothing()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);

        var processedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "entry-events",
            Payload = "{}",
            ProcessedAt = DateTime.UtcNow
        };
        dbContext.OutboxMessages.Add(processedMessage);
        await dbContext.SaveChangesAsync();

        var scopeFactory = SetupScopeFactory(dbContext);
        var worker = new TestableOutboxBackgroundWorker(scopeFactory, _mockLogger.Object);

        // Act
        await worker.InvokeProcessAsync(CancellationToken.None);


        _mockMessageBus.Verify(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private class TestableOutboxBackgroundWorker : OutboxBackgroundWorker
    {
        public TestableOutboxBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundWorker> logger)
            : base(scopeFactory, logger) { }

        public async Task InvokeProcessAsync(CancellationToken token)
        {
            var method = typeof(OutboxBackgroundWorker).GetMethod("ProcessOutboxMessagesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                var task = (Task)method.Invoke(this, new object[] { token })!;
                await task;
            }
        }
    }
}