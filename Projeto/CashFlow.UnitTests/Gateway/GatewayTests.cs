using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CashFlow.UnitTests.Gateway;

// IClassFixture levanta o servidor do Gateway (Program.cs) uma única vez para todos os testes
public class GatewayTests : IClassFixture<WebApplicationFactory<CashFlow.Gateway.Program>>
{
    private readonly HttpClient _client;

    public GatewayTests(WebApplicationFactory<CashFlow.Gateway.Program> factory)
    {
        // Cria um cliente HTTP que aponta diretamente para o Gateway em memória
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task YarpRouting_WhenDownstreamIsOffline_ShouldReturnBadGateway()
    {
        // Arrange: Prepara uma requisição para a rota de saldo configurada no YARP
        var requestUri = "/api/balance/2026-10-12";

        // Act: Envia a requisição
        var response = await _client.GetAsync(requestUri);

        // Assert: Validação Arquitetural
        // O Gateway aceitou a rota (não deu 404), tentou redirecionar para a porta 5002 (ConsolidationAPI),
        // mas como as APIs reais não estão a correr no teste, o YARP devolve corretamente 502 Bad Gateway.
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task RateLimiter_WhenExceeding50RequestsPerSecond_ShouldReturnTooManyRequests()
    {
        // Arrange: Dispara 100 requisições simultâneas. 
        // Como o limite é 50 por segundo, uma rajada de 100 garante matematicamente 
        // que esgotaremos a quota antes que o segundo termine, mesmo em máquinas mais lentas.
        var requestCount = 100;
        var tasks = new Task<HttpResponseMessage>[requestCount];

        // Act: Envia todas as requisições ao mesmo tempo (em paralelo)
        for (int i = 0; i < requestCount; i++)
        {
            tasks[i] = _client.GetAsync("/api/balance/2026-10-12");
        }

        var responses = await Task.WhenAll(tasks);

        // Assert: Valida se a barreira de proteção funcionou
        // Procuramos na lista de respostas se PELO MENOS UMA foi intercetada com HTTP 429
        bool hasBlockedRequest = false;
        foreach (var response in responses)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                hasBlockedRequest = true;
                break;
            }
        }

        Assert.True(hasBlockedRequest, "O Rate Limiter não bloqueou as requisições. O limite de 50 RPS não foi atingido ou a configuração falhou.");
    }

    [Fact]
    public async Task YarpRouting_UnmappedRoute_ShouldReturnNotFound()
    {
        // Act: Tenta aceder a uma rota que não existe no appsettings.json do YARP
        var response = await _client.GetAsync("/api/rota-inexistente");

        // Assert: O Gateway rejeita imediatamente com 404 sem tentar procurar portas internas
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}