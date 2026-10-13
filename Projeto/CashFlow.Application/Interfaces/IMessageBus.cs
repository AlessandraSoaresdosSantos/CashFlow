using System.Threading.Tasks;

namespace CashFlow.Application.Interfaces;

public interface IMessageBus
{
    // Método genérico para publicar qualquer evento no Kafka
    Task PublishAsync<T>(string topic, T message);
}