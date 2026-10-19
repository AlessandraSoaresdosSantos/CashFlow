using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CashFlow.UnitTests.Gateway;

public class GatewayTests : IClassFixture<WebApplicationFactory<CashFlow.Gateway.Program>>
{
    private readonly HttpClient _client;

    public GatewayTests(WebApplicationFactory<CashFlow.Gateway.Program> factory)
    {
        
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task YarpRouting_WhenDownstreamIsOffline_ShouldReturnBadGateway()
    {
        // Arrange: Prepara uma requisição para a rota de saldo configurada no YARP
        var requestUri = "/api/balance/2026-10-12";

        // Act: Envia a requisição
        var response = await _client.GetAsync(requestUri);

               Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task RateLimiter_WhenExceeding50RequestsPerSecond_ShouldReturnTooManyRequests()
    {
        var requestCount = 100;
        var tasks = new Task<HttpResponseMessage>[requestCount];

        // Act: Envia todas as requisições ao mesmo tempo (em paralelo)
        for (int i = 0; i < requestCount; i++)
        {
            tasks[i] = _client.GetAsync("/api/balance/2026-10-12");
        }

        var responses = await Task.WhenAll(tasks);

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
         var response = await _client.GetAsync("/api/rota-inexistente");

         Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}