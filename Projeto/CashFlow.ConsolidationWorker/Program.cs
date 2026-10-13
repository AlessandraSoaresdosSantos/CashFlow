using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using StackExchange.Redis;

namespace CashFlow.ConsolidationWorker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Regista as ligações como Singleton (uma única ligação partilhada por toda a app)
        builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

        // Regista o Worker que vai correr em background
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}