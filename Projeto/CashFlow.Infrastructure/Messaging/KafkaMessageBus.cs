using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;

namespace CashFlow.Infrastructure.Messaging;

public class KafkaMessageBus : IMessageBus
{
    private readonly ILogger<KafkaMessageBus> _logger;
    private readonly string _bootstrapServers;

    public KafkaMessageBus(ILogger<KafkaMessageBus> logger, IConfiguration configuration)
    {
        _logger = logger;

        _bootstrapServers = configuration["Kafka:BootstrapServers"]
                             ?? throw new ArgumentNullException("Kafka:BootstrapServers não configurado no appsettings.json.");
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            var payload = typeof(T) == typeof(string) ? message as string : System.Text.Json.JsonSerializer.Serialize(message);

            await producer.ProduceAsync(topic, new Message<Null, string> { Value = payload });
            _logger.LogInformation("Mensagem publicada com sucesso no tópico {Topic}", topic);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Falha ao comunicar com o Kafka no tópico {Topic}.", topic);
            throw; 
        }
    }
}