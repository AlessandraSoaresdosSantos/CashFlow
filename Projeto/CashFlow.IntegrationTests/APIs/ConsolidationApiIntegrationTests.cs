using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.IntegrationTests.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CashFlow.IntegrationTests.APIs;

public class ConsolidationApiIntegrationTests : IClassFixture<WebApplicationFactory<CashFlow.ConsolidationAPI.Program>>
{
    private readonly HttpClient _client;

    public ConsolidationApiIntegrationTests(WebApplicationFactory<CashFlow.ConsolidationAPI.Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 1. Remove a dependência real do Redis
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBalanceRepository));
                if (redisDescriptor != null) services.Remove(redisDescriptor);

                // 2. Adiciona um Mock que simula o Redis a devolver um saldo de R$ 2500,00
                var mockRedisRepo = new Mock<IBalanceRepository>();
                mockRedisRepo.Setup(r => r.GetBalanceAsync("2026-10-12")).ReturnsAsync(2500.00);
                services.AddScoped(_ => mockRedisRepo.Object);

                // 3. Injeta a Autenticação Falsa
                services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });

        _client = customFactory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task GetBalance_ForSpecificDate_ShouldReturnOkWithCorrectBalance()
    {
        // Arrange
        var requestDate = "2026-10-12";

        // Act
        var response = await _client.GetAsync($"/api/balance/{requestDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Lê e valida o JSON devolvido
        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseString);
        var root = jsonDocument.RootElement;

        Assert.Equal(requestDate, root.GetProperty("date").GetString());
        Assert.Equal(2500.00, root.GetProperty("balance").GetDouble());
    }
}