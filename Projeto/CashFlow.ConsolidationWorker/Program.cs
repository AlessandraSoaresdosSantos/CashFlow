using CashFlow.ConsolidationWorker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using StackExchange.Redis;

namespace CashFlow.ConsolidationWorker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Garante que o appsettings.json e variáveis de ambiente sejam carregados corretamente
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // 1. Vincula as configurações do WorkerOptions através do padrão de opções (.NET Options Pattern)
                services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));

                // 2. Regista o cliente do MongoDB como Singleton (gerido internamente pelo driver)
                var mongoConnectionString = configuration.GetConnectionString("MongoConnection") ?? "mongodb://localhost:27017";
                services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

                // 3. Regista o Multiplexer do Redis para conexão com a cache
                var redisConnectionString = configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

                // 4. Regista o Worker principal como Background Service Hosted Service
                services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}