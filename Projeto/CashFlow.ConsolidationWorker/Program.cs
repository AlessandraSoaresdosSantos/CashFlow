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
                
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                
                services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));

                var mongoConnectionString = configuration.GetConnectionString("MongoConnection") ?? "mongodb://localhost:27017";
                services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

                var redisConnectionString = configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

               services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}