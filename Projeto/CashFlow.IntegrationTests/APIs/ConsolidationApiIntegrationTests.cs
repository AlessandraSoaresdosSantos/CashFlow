using CashFlow.Application.Interfaces;
using CashFlow.ConsolidationAPI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CashFlow.IntegrationTests;

public class ConsolidationApiIntegrationTests : IClassFixture<WebApplicationFactory<CashFlow.ConsolidationAPI.Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IBalanceRepository> _mockRepository;

    public ConsolidationApiIntegrationTests(WebApplicationFactory<CashFlow.ConsolidationAPI.Program> factory)
    {
        _mockRepository = new Mock<IBalanceRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            { 
                services.RemoveAll(typeof(IBalanceRepository));
                 
                services.AddScoped(_ => _mockRepository.Object);
                 
                services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
                 
                var hostedServiceDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType?.Name.Contains("KafkaConsumerWorker") == true);
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }
            });
        });
    }

    [Fact]
    public async Task GetBalance_WhenRequested_ShouldReturnOkAndTotalAmount()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedBalance = 7500.50m;
        var dateParameter = "2026-10-18";

        _mockRepository.Setup(r => r.GetBalanceAsync(dateParameter))
             .ReturnsAsync(expectedBalance);

        // Act
        var response = await client.GetAsync($"/api/balance/{dateParameter}");

        // Garante que se rebentar com erro 500, o teste mostra a mensagem
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"A API retornou {response.StatusCode}. Detalhe: {responseContent}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verifica o corpo da resposta HTTP
        Assert.Contains("7500.50", responseContent);
    }
}
 
public class FakePolicyEvaluator : IPolicyEvaluator
{
    public virtual async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        var principal = new ClaimsPrincipal();
        principal.AddIdentity(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "IntegrationTestUser")
        }, "FakeScheme"));

        return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,
            new AuthenticationProperties(), "FakeScheme")));
    }

    public virtual async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy,
        AuthenticateResult authenticationResult, HttpContext context, object resource)
    {
        return await Task.FromResult(PolicyAuthorizationResult.Success());
    }
}