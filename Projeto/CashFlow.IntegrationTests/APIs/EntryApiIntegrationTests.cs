using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Infrastructure.Data.Relational;
using CashFlow.IntegrationTests.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CashFlow.IntegrationTests.APIs;

public class EntryApiIntegrationTests : IClassFixture<WebApplicationFactory<CashFlow.EntryAPI.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<CashFlow.EntryAPI.Program> _factory;

    public EntryApiIntegrationTests(WebApplicationFactory<CashFlow.EntryAPI.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 1. Remove qualquer opção anterior
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // 2. A "BALA DE PRATA": Isolamento de Provedor (Service Provider Isolation)
                // Isto cria um mini-motor de injeção fechado apenas para o banco em memória,
                // impedindo totalmente o conflito com o SQL Server do Program.cs.
                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationDbForEntry")
                           .UseInternalServiceProvider(inMemoryServiceProvider); // <-- Força a usar a bolha
                });

                // 3. Substitui o Kafka por um Mock
                var kafkaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMessageBus));
                if (kafkaDescriptor != null) services.Remove(kafkaDescriptor);
                services.AddSingleton(new Mock<IMessageBus>().Object);

                // 4. Injeta a Autenticação Falsa
                services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // 5. Força a aplicação a ignorar o "Bearer"
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task PostEntry_WithValidData_ShouldReturnCreatedAndSaveToDatabase()
    {
        // Arrange
        var command = new CreateEntryCommand("Venda de Pudim Tradicional", 45.50m, 1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/entry", command);
        var erroDaApi = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"FALHA NA API! Status retornado: {response.StatusCode} | Mensagem da API: {erroDaApi}");

        // Validação extra: Banco de dados
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedEntry = await dbContext.Entries.FirstOrDefaultAsync(e => e.Description == "Venda de Pudim Tradicional");

        Assert.NotNull(savedEntry);
        Assert.Equal(45.50m, savedEntry.Amount);
    }
}