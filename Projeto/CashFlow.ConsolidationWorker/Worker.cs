using CashFlow.Domain.Events;
using Confluent.Kafka;
using System.Text.Json;

namespace CashFlow.ConsolidationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic = "entry-events-topic";

    public Worker(
        ILogger<Worker> logger,
        IConsumer<Ignore, string> consumer,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _consumer = consumer;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de Consolidação iniciado. A inscrever-se no tópico {Topic}...", _topic);
        _consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message != null)
                    {
                        var messageValue = consumeResult.Message.Value;
                        _logger.LogInformation("Mensagem recebida do Kafka: {Message}", messageValue);

                        await ProcessMessageAsync(messageValue, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Erro interno do Kafka ao consumir a mensagem.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processo de consumo cancelado (Shutdown da aplicação).");
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Conexão com o Kafka encerrada de forma segura.");
        }
    }

    public async Task ProcessMessageAsync(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var entryEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(messageValue);
            if (entryEvent == null)
            {
                _logger.LogWarning("A mensagem recebida não pôde ser desserializada.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();

            _logger.LogInformation("Lançamento {EntryId} consolidado com sucesso no MongoDB e Redis.", entryEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao tentar processar e consolidar a mensagem: {Message}", messageValue);

        }
    }
}