using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using StackExchange.Redis;

namespace CashFlow.Infrastructure.Data.Cache;

public class RedisBalanceRepository : IBalanceRepository
{
    private readonly IConnectionMultiplexer _redis;

    public RedisBalanceRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<double> GetBalanceAsync(string date)
    {
        var db = _redis.GetDatabase();

        // A mesma chave que o ConsolidationWorker utiliza para gravar
        var redisKey = $"balance:{date}";

        var value = await db.StringGetAsync(redisKey);

        // Se existir valor, converte e devolve. Caso contrário, devolve 0.
        if (value.HasValue && double.TryParse(value, out double balance))
        {
            return balance;
        }

        return 0.0;
    }
}