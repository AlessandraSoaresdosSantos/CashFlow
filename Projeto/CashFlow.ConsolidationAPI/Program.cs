using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using MongoDB.Driver;
using System;
using CashFlow.Application.Interfaces;
using CashFlow.Infrastructure.Data.Caching;
using CashFlow.Application.UseCases.Handlers;

namespace CashFlow.ConsolidationAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
        {
            options.Authority = builder.Configuration["Keycloak:Authority"];
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

        var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new ArgumentNullException(nameof(redisConnectionString), "A connection string do Redis não foi configurada.");
        }

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

    
        var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDbConnection");
        builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "CashFlowConsolidatedDb";
            return client.GetDatabase(databaseName);
        });

        builder.Services.AddScoped<IBalanceRepository, RedisBalanceRepository>();
        builder.Services.AddScoped<IGetBalanceQueryHandler, GetBalanceQueryHandler>();

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}