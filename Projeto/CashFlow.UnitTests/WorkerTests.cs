using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashFlow.ConsolidationWorker;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CashFlow.UnitTests.Workers;

public class WorkerTests
{
    [Fact]
    public async Task ProcessMessageAsync_ValidCreditMessage_ShouldSaveToMongoAndIncrementRedis()
    {
        // Arrange: Preparação dos Mocks (Falsificações de Bancos de Dados)
        var mockLogger = new Mock<ILogger<Worker>>();

        // Mock do MongoDB
        var mockMongoClient = new Mock<IMongoClient>();
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<BsonDocument>>();

        mockMongoClient.Setup(m => m.GetDatabase("CashFlowDb", null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<BsonDocument>("EntrySnapshots", null)).Returns(mockCollection.Object);

        // Mock do Redis
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockRedisDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), null)).Returns(mockRedisDb.Object);

        // Instancia o Worker injetando os mocks (sem conectar de verdade)
        var worker = new Worker(mockLogger.Object, mockMongoClient.Object, mockRedis.Object);

        // Simula um evento recebido do Kafka (Um crédito de R$ 350.00 das Vendas do Pudim)
        var entryEvent = new EntryEventDto
        {
            Id = Guid.NewGuid(),
            Amount = 350.00m,
            Type = 1, // 1 = Crédito
            CreatedAt = DateTime.UtcNow
        };
        var messageJson = JsonSerializer.Serialize(entryEvent);

        // Act: Executa o método de processamento do Worker
        await worker.ProcessMessageAsync(messageJson);

        // Assert: Validações Arquiteturais

        // 1. Verifica se chamou a inserção do MongoDB exatamente 1 vez
        mockCollection.Verify(c => c.InsertOneAsync(
            It.IsAny<BsonDocument>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Verifica se incrementou o saldo positivamente no Redis exatamente 1 vez
        string expectedKey = $"balance:{entryEvent.CreatedAt:yyyy-MM-dd}";
        mockRedisDb.Verify(db => db.StringIncrementAsync(
            expectedKey,
            350.0, // Verifica se repassou o valor 350 corretamente
            CommandFlags.None), Times.Once);
    }
}