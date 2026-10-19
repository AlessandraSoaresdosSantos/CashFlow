using CashFlow.ConsolidationWorker;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CashFlow.ConsolidationWorker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var consumerConfig = new ConsumerConfig
                {
                   BootstrapServers = hostContext.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092",

                   GroupId = hostContext.Configuration["Kafka:GroupId"] ?? "cashflow-consolidation-group",

                   AutoOffsetReset = AutoOffsetReset.Earliest
                };

                services.AddSingleton<IConsumer<Ignore, string>>(sp =>
                {
                    return new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                });

              services.AddHostedService<Worker>();
            });
}