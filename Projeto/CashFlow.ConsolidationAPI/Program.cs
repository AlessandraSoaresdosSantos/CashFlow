using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Queries;
using CashFlow.Infrastructure.Data.Cache;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace CashFlow.ConsolidationAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Configuração de Autenticação JWT (Keycloak)
        builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
        {
            options.Authority = "http://localhost:8080/realms/cashflow";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

        // 2. Injeção de Dependências (Infraestrutura e CQRS)
        // Regista a ligação ao Redis como Singleton
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect("localhost:6379"));

        // Regista o repositório e o handler
        builder.Services.AddScoped<IBalanceRepository, RedisBalanceRepository>();
        builder.Services.AddScoped<GetBalanceQueryHandler>();

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}