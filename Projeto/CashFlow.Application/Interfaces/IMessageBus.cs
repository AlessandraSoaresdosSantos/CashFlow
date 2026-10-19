using System.Threading.Tasks;

namespace CashFlow.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message);
}