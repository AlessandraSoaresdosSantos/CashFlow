using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Infrastructure.Data.Relational;
using CashFlow.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.EntryAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

    
        var connectionString = "Server=localhost,1433;Database=CashFlowDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";
        builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));

        builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
        {
            options.Authority = "http://localhost:8080/realms/cashflow";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

        builder.Services.AddScoped<IEntryRepository, EntryRepository>();
        builder.Services.AddScoped<ICreateEntryCommandHandler, CreateEntryCommandHandler>();
        builder.Services.AddScoped<IMessageBus, KafkaMessageBus>();
        builder.Services.AddHostedService<Workers.OutboxBackgroundWorker>();

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}