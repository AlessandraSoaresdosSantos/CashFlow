using System;
using System.Threading;
using System.Threading.Tasks;
using CashFlow.ConsolidationWorker;
using CashFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CashFlow.UnitTests.Workers;

public class WorkerTests
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IMongoClient> _mockMongoClient;
    private readonly Mock<IMongoDatabase> _mockMongoDatabase;
    private readonly Mock<IMongoCollection<BsonDocument>> _mockMongoCollection;
    private readonly Mock<IConnectionMultiplexer> _mockRedisMultiplexer;
    private readonly Mock<IDatabase> _mockRedisDatabase;
    private readonly IOptions<WorkerOptions> _workerOptions;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockMongoClient = new Mock<IMongoClient>();
        _mockMongoDatabase = new Mock<IMongoDatabase>();
        _mockMongoCollection = new Mock<IMongoCollection<BsonDocument>>();
        _mockRedisMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockRedisDatabase = new Mock<IDatabase>();

        var options = new WorkerOptions
        {
            KafkaBootstrapServers = "localhost:9092",
            TopicName = "entry-events",
            DeadLetterTopicName = "entry-events-dlt",
            DatabaseName = "CashFlowDb",
            CollectionName = "EntrySnapshots"
        };
        _workerOptions = Options.Create(options);

        _mockMongoClient.Setup(c => c.GetDatabase(options.DatabaseName, null)).Returns(_mockMongoDatabase.Object);
        _mockMongoDatabase.Setup(d => d.GetCollection<BsonDocument>(options.CollectionName, null)).Returns(_mockMongoCollection.Object);
        _mockRedisMultiplexer.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDatabase.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenMessageIsValidAndNotProcessed_ShouldSaveToMongoAndIncrementRedis()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var messageJson = $@"{{
            ""Id"": ""{entryId}"",
            ""Amount"": 150.00,
            ""Type"": {(int)EntryType.Credit},
            ""CreatedAt"": ""2026-10-12T10:00:00Z""
        }}";

        _mockMongoCollection
            .Setup(c => c.FindAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<FindOptions<BsonDocument, BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncCursor<BsonDocument>>(cursor => cursor.Current == Array.Empty<BsonDocument>() && cursor.MoveNext(It.IsAny<CancellationToken>()) == false));

        var worker = new Worker(
            _mockLogger.Object,
            _mockMongoClient.Object,
            _mockRedisMultiplexer.Object,
            _workerOptions
        );

        // Act
        await worker.ProcessMessageAsync(messageJson);

        // Assert
        _mockMongoCollection.Verify(c => c.InsertOneAsync(It.IsAny<BsonDocument>(), null, It.IsAny<CancellationToken>()), Times.Once);

        // Utiliza It.IsAny<RedisKey>() em vez de string
        _mockRedisDatabase.Verify(r => r.StringIncrementAsync(It.IsAny<RedisKey>(), 150.00, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenMessageIsDuplicate_ShouldIgnoreProcessing()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var messageJson = $@"{{
            ""Id"": ""{entryId}"",
            ""Amount"": 100.00,
            ""Type"": {(int)EntryType.Debit},
            ""CreatedAt"": ""2026-10-12T10:00:00Z""
        }}";

        var existingDoc = new BsonDocument { { "Id", entryId.ToString() } };
        var asyncCursorMock = new Mock<IAsyncCursor<BsonDocument>>();
        asyncCursorMock.Setup(c => c.Current).Returns(new[] { existingDoc });
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockMongoCollection
            .Setup(c => c.FindAsync(It.IsAny<FilterDefinition<BsonDocument>>(), It.IsAny<FindOptions<BsonDocument, BsonDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asyncCursorMock.Object);

        var worker = new Worker(
            _mockLogger.Object,
            _mockMongoClient.Object,
            _mockRedisMultiplexer.Object,
            _workerOptions
        );

        // Act
        await worker.ProcessMessageAsync(messageJson);

        // Assert
        _mockMongoCollection.Verify(c => c.InsertOneAsync(It.IsAny<BsonDocument>(), null, It.IsAny<CancellationToken>()), Times.Never);
        _mockRedisDatabase.Verify(r => r.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenMessageIsMalformed_ShouldNotThrowAndLogHandling()
    {
        // Arrange
        var malformedJson = "{ invalid_json_payload }";

        var worker = new Worker(
            _mockLogger.Object,
            _mockMongoClient.Object,
            _mockRedisMultiplexer.Object,
            _workerOptions
        );

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => worker.ProcessMessageAsync(malformedJson));
        Assert.Null(exception);

        _mockMongoCollection.Verify(c => c.InsertOneAsync(It.IsAny<BsonDocument>(), null, It.IsAny<CancellationToken>()), Times.Never);
        _mockRedisDatabase.Verify(r => r.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<CommandFlags>()), Times.Never);
    }
}