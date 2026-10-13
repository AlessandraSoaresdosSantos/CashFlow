using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CashFlow.ConsolidationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMongoClient _mongoClient;
    private readonly IConnectionMultiplexer _redis;

    private const string KafkaServer = "localhost:9092";
    private const string TopicName = "entry-events";

    // Recebe as dependências injetadas (perfeito para Mocks no Teste)
    public Worker(ILogger<Worker> logger, IMongoClient mongoClient, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _mongoClient = mongoClient;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig { BootstrapServers = KafkaServer, GroupId = "consolidation-worker", AutoOffsetReset = AutoOffsetReset.Earliest };
        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(TopicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    await ProcessMessageAsync(consumeResult.Message.Value);
                }
            }
        }
        catch (OperationCanceledException) { consumer.Close(); }
    }

    // Método Público para ser testado isoladamente pelo xUnit
    public async Task ProcessMessageAsync(string messageJson)
    {
        var entryEvent = JsonSerializer.Deserialize<EntryEventDto>(messageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (entryEvent != null)
        {
            await SaveToMongoAsync(messageJson);
            await UpdateRedisBalanceAsync(entryEvent);
        }
    }

    private async Task SaveToMongoAsync(string messageJson)
    {
        var database = _mongoClient.GetDatabase("CashFlowDb");
        var collection = database.GetCollection<BsonDocument>("EntrySnapshots");

        var document = BsonDocument.Parse(messageJson);
        document.Add("ProcessedAt", DateTime.UtcNow);

        await collection.InsertOneAsync(document);
    }

    private async Task UpdateRedisBalanceAsync(EntryEventDto entryEvent)
    {
        var db = _redis.GetDatabase();
        string dateKey = entryEvent.CreatedAt.ToString("yyyy-MM-dd");
        string redisKey = $"balance:{dateKey}";

        double amountToApply = entryEvent.Type == 1 ? (double)entryEvent.Amount : -(double)entryEvent.Amount;
        await db.StringIncrementAsync(redisKey, amountToApply);
    }
}

public class EntryEventDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public int Type { get; set; }
    public DateTime CreatedAt { get; set; }
}