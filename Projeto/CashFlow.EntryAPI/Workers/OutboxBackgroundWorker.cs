using CashFlow.Application.Interfaces;
using CashFlow.Infrastructure.Data.Relational;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.EntryAPI.Workers;

public class OutboxBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundWorker> _logger;

    public OutboxBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundWorker iniciado. A verificar pendências na base de dados...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao tentar processar mensagens do Outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(stoppingToken);

        if (!pendingMessages.Any())
            return;

        _logger.LogInformation("Processando {Count} mensagens pendentes no Outbox...", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
               await messageBus.PublishAsync(message.Topic, message.Payload);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Falha ao enviar mensagem Outbox {Id}. O Kafka pode estar indisponível. Tentaremos de novo mais tarde.", message.Id);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}