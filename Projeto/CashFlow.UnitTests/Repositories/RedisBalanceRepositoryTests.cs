using CashFlow.Domain.Entities;
using CashFlow.Infrastructure.Data.Caching;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CashFlow.UnitTests.Repositories;

public class RedisBalanceRepositoryTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedisMultiplexer;
    private readonly Mock<IDatabase> _mockRedisDb;
    private readonly Mock<IMongoDatabase> _mockMongoDatabase;
    private readonly Mock<IMongoCollection<ConsolidatedBalance>> _mockMongoCollection;
    private readonly Mock<ILogger<RedisBalanceRepository>> _mockLogger;

    public RedisBalanceRepositoryTests()
    {
        _mockRedisMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockRedisDb = new Mock<IDatabase>();
        _mockRedisMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

        _mockMongoDatabase = new Mock<IMongoDatabase>();
        _mockMongoCollection = new Mock<IMongoCollection<ConsolidatedBalance>>();
        _mockMongoDatabase.Setup(m => m.GetCollection<ConsolidatedBalance>(It.IsAny<string>(), null)).Returns(_mockMongoCollection.Object);

        _mockLogger = new Mock<ILogger<RedisBalanceRepository>>();
    }

    private void SetupMongoReturns(decimal amount)
    {
        var mockCursor = new Mock<IAsyncCursor<ConsolidatedBalance>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(new List<ConsolidatedBalance> { new ConsolidatedBalance { TotalAmount = amount, Id = "2026-10-18" } });

        _mockMongoCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<ConsolidatedBalance>>(),
            It.IsAny<FindOptions<ConsolidatedBalance, ConsolidatedBalance>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenRedisHasData_ShouldReturnFromRedisAndNotCallMongo()
    {
        // Arrange
        var expectedBalance = 500.50m;
        var parameter = "2026-10-18";
        var cacheKey = $"balance:{parameter}";

        _mockRedisDb.Setup(db => db.StringGetAsync(cacheKey, CommandFlags.None))
            .ReturnsAsync(new RedisValue(expectedBalance.ToString()));

        var repository = new RedisBalanceRepository(_mockRedisMultiplexer.Object, _mockMongoDatabase.Object, _mockLogger.Object);

        // Act
        var result = await repository.GetBalanceAsync(parameter);

        // Assert
        Assert.Equal(expectedBalance, result);
        _mockMongoCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<ConsolidatedBalance>>(), It.IsAny<FindOptions<ConsolidatedBalance, ConsolidatedBalance>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenRedisIsEmpty_ShouldCallMongo()
    {
        // Arrange
        var expectedBalance = 1500.75m;
        var parameter = "2026-10-18";
        var cacheKey = $"balance:{parameter}";

       
        _mockRedisDb.Setup(db => db.StringGetAsync(cacheKey, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);


        SetupMongoReturns(expectedBalance);

        var repository = new RedisBalanceRepository(_mockRedisMultiplexer.Object, _mockMongoDatabase.Object, _mockLogger.Object);

        // Act
        var result = await repository.GetBalanceAsync(parameter);

        // Assert
        Assert.Equal(expectedBalance, result);
          
        _mockMongoCollection.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<ConsolidatedBalance>>(),
            It.IsAny<FindOptions<ConsolidatedBalance, ConsolidatedBalance>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenRedisThrowsException_ShouldGracefullyFallbackToMongo()
    {
        // Arrange
        var expectedBalance = 3200.00m;
        var parameter = "2026-10-18";
        var cacheKey = $"balance:{parameter}";

        _mockRedisDb.Setup(db => db.StringGetAsync(cacheKey, CommandFlags.None))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Servidor Offline"));

        SetupMongoReturns(expectedBalance);

        var repository = new RedisBalanceRepository(_mockRedisMultiplexer.Object, _mockMongoDatabase.Object, _mockLogger.Object);

        // Act
        var result = await repository.GetBalanceAsync(parameter);

        // Assert
        Assert.Equal(expectedBalance, result);
    }
}