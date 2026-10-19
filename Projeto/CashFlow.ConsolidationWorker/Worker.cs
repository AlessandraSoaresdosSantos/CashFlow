using Confluent.Kafka;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text.Json;
using CashFlow.Domain.Enums;

namespace CashFlow.ConsolidationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMongoClient _mongoClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly WorkerOptions _options;
    private readonly IProducer<Null, string> _deadLetterProducer;

    public Worker(
        ILogger<Worker> logger,
        IMongoClient mongoClient,
        IConnectionMultiplexer redis,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _mongoClient = mongoClient;
        _redis = redis;
        _options = options.Value;

        var producerConfig = new ProducerConfig { BootstrapServers = _options.KafkaBootstrapServers };
        _deadLetterProducer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.KafkaBootstrapServers,
            GroupId = "consolidation-worker",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_options.TopicName);

        _logger.LogInformation("ConsolidationWorker ativo e processando eventos do Kafka no tópico: {Topic}", _options.TopicName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    await ProcessMessageAsync(consumeResult.Message.Value);
                    consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Erro de consumo no Kafka: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico não tratado no ciclo de execução do Worker.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    public async Task ProcessMessageAsync(string messageJson)
    {
        EntryEventDto? entryEvent = null;

        // 1. Resiliência: Tratamento de JSON inválido com desvio seguro para a DLT
        try
        {
            entryEvent = JsonSerializer.Deserialize<EntryEventDto>(messageJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (entryEvent == null || entryEvent.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Payload vazio, nulo ou com ID de transação inválido.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mensagem malformada detetada. Redirecionando para a Dead-Letter Topic (DLT).");
            await SendToDeadLetterQueueAsync(messageJson, ex.Message);
            return;
        }

        // 2. Idempotência: Verificação de duplicidade no MongoDB
        var database = _mongoClient.GetDatabase(_options.DatabaseName);
        var collection = database.GetCollection<BsonDocument>(_options.CollectionName);

        var existingSnapshot = await collection.Find(Builders<BsonDocument>.Filter.Eq("Id", entryEvent.Id.ToString())).FirstOrDefaultAsync();
        if (existingSnapshot != null)
        {
            _logger.LogWarning("Evento duplicado detetado (ID: {Id}). Processamento ignorado por idempotência.", entryEvent.Id);
            return;
        }

        // Persistência do Snapshot no MongoDB
        var document = BsonDocument.Parse(messageJson);
        document.Set("ProcessedAt", DateTime.UtcNow);
        await collection.InsertOneAsync(document);

        // 3. Atualização Atômica e Segura no Redis (Com correção do bug de tipo)
        await UpdateRedisBalanceAsync(entryEvent);
    }

    private async Task UpdateRedisBalanceAsync(EntryEventDto entryEvent)
    {
        var db = _redis.GetDatabase();
        string dateKey = entryEvent.CreatedAt.ToString("yyyy-MM-dd");
        string redisKey = $"balance:{dateKey}";

        double amountToApply;

        // Validação estrita e defensiva do tipo de lançamento
        if (entryEvent.Type == EntryType.Credit)
        {
            amountToApply = (double)entryEvent.Amount;
        }
        else if (entryEvent.Type == EntryType.Debit)
        {
            amountToApply = -(double)entryEvent.Amount;
        }
        else
        {
            _logger.LogWarning("Tipo de lançamento financeiro desconhecido ({Type}) para o evento {Id}. Nenhuma alteração aplicada ao saldo.", entryEvent.Type, entryEvent.Id);
            return;
        }

        await db.StringIncrementAsync(redisKey, amountToApply);
        _logger.LogInformation("Saldo consolidado com sucesso no Redis para a chave {Key}. Montante aplicado: {Amount}", redisKey, amountToApply);
    }

    private async Task SendToDeadLetterQueueAsync(string originalMessage, string errorMessage)
    {
        try
        {
            var dltPayload = JsonSerializer.Serialize(new
            {
                OriginalPayload = originalMessage,
                Error = errorMessage,
                FailedAt = DateTime.UtcNow
            });

            var dltMessage = new Message<Null, string> { Value = dltPayload };
            await _deadLetterProducer.ProduceAsync(_options.DeadLetterTopicName, dltMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha crítica ao tentar publicar mensagem na Dead-Letter Topic.");
        }
    }
}

public class EntryEventDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public EntryType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}