using System;
using System.Text.Json;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Messaging;

public class KafkaMessageBus : IMessageBus, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaMessageBus> _logger;

    public KafkaMessageBus(ILogger<KafkaMessageBus> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            // Configurações para garantir a entrega (Resiliência)
            Acks = Acks.All,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<Null, string> { Value = messageJson };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

            _logger.LogInformation($"Evento publicado no tópico {deliveryResult.Topic}. Offset: {deliveryResult.Offset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError($"Falha ao publicar no Kafka: {ex.Error.Reason}");
            throw; // Em produção, isto ativaria um padrão Outbox/Retry
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}