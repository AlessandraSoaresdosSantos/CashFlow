using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Infrastructure.Data.Relational;
using CashFlow.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.EntryAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Configuração da Base de Dados SQL Server
        var connectionString = "Server=localhost,1433;Database=CashFlowDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";
        builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));

        // 2. Configuração de Autenticação JWT (Keycloak)
        builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
        {
            options.Authority = "http://localhost:8080/realms/cashflow";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

        // 3. Injeção de Dependências
        builder.Services.AddScoped<IEntryRepository, EntryRepository>();
        builder.Services.AddSingleton<IMessageBus, KafkaMessageBus>(); // Singleton para manter a ligação aberta
        builder.Services.AddScoped<CreateEntryCommandHandler>();

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}