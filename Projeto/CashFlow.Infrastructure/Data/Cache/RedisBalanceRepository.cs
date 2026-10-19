using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace CashFlow.Infrastructure.Data.Caching;

public class RedisBalanceRepository : IBalanceRepository
{
    private readonly IDatabase _redis;
    private readonly IMongoCollection<ConsolidatedBalance> _mongoCollection;
    private readonly ILogger<RedisBalanceRepository> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public RedisBalanceRepository(
        IConnectionMultiplexer redisMultiplexer,
        IMongoDatabase mongoDatabase,
        ILogger<RedisBalanceRepository> logger)
    {
        _redis = redisMultiplexer.GetDatabase();
        _mongoCollection = mongoDatabase.GetCollection<ConsolidatedBalance>("ConsolidatedBalances");
        _logger = logger;
    }

    public async Task<decimal> GetBalanceAsync(string parameter)
    {
       var cacheKey = $"balance:{parameter}";

        try
        {
            var cachedBalance = await _redis.StringGetAsync(cacheKey);

            if (cachedBalance.HasValue)
            {
                _logger.LogInformation("Saldo recuperado do cache Redis para a chave {Key}.", cacheKey);
                return decimal.Parse(cachedBalance!);
            }

            _logger.LogInformation("Cache miss no Redis para {Key}. Iniciando fallback para o MongoDB.", cacheKey);
        }
        catch (Exception ex) when (ex is RedisConnectionException || ex is TimeoutException)
        {
            _logger.LogWarning(ex, "Redis incontactável/offline. Acionando fallback para o MongoDB.");
        }

        var mongoDocument = await _mongoCollection.Find(doc => doc.Id == parameter).FirstOrDefaultAsync();
        var totalBalance = mongoDocument?.TotalAmount ?? 0m;

        try
        {
            await _redis.StringSetAsync(cacheKey, totalBalance.ToString(), CacheTtl);
            _logger.LogInformation("Cache do Redis reabastecido com sucesso após fallback.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao tentar reabastecer o Redis. O fluxo não será interrompido.");
        }

        return totalBalance;
    }
}