using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System;
using Microsoft.AspNetCore.Http;

namespace CashFlow.Gateway;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Configuração do YARP (Lê as rotas do appsettings.json)
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // 2. Configuração de Autenticação (Zero-Trust)
        // O Gateway valida o token do utilizador antes de permitir a passagem para as APIs internas
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "http://localhost:8080/realms/cashflow";
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });
        builder.Services.AddAuthorization();

        // 3. Rate Limiting (Proteção contra Sobrecarga / DDoS)
        // Limita os utilizadores a 50 requisições por segundo (Requisito RNF-01 da Arquitetura)
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Agrupa o limite pelo Token do Utilizador ou pelo IP
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 50, // Limite de RPS
                        Window = TimeSpan.FromSeconds(1)
                    }));

            // Retorna HTTP 429 se o utilizador exceder o limite
            options.RejectionStatusCode = 429;
        });

        var app = builder.Build();

        // Ativa o bloqueio por excesso de requisições
        app.UseRateLimiter();

        // Ativa a verificação de segurança
        app.UseAuthentication();
        app.UseAuthorization();

        // Inicia o motor de reencaminhamento de rotas
        app.MapReverseProxy();

        app.Run();
    }
}