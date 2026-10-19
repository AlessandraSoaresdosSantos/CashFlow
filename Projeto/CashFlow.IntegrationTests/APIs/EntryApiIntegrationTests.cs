using CashFlow.Application.UseCases.Commands;
using CashFlow.Domain.Enums;
using CashFlow.EntryAPI;
using CashFlow.Infrastructure.Data.Relational;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace CashFlow.IntegrationTests;

public class EntryApiIntegrationTests : IClassFixture<WebApplicationFactory<CashFlow.EntryAPI.Program>>
{
    private readonly WebApplicationFactory<CashFlow.EntryAPI.Program> _factory;

    public EntryApiIntegrationTests(WebApplicationFactory<CashFlow.EntryAPI.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(DbContextOptions));

     
                var efServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

               
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestsDb");
                    options.UseInternalServiceProvider(efServiceProvider); 
                });

               
                var hostedServiceDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(CashFlow.EntryAPI.Workers.OutboxBackgroundWorker));
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }
            });
        });
    }

    [Fact]
    public async Task PostEntry_WhenPayloadIsValid_ShouldReturnCreatedAndPersistInOutbox()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new CreateEntryCommand
        {
            Description = "Pagamento da internet",
            Amount = 150.50m,
            Type = EntryType.Debit
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/entry", command);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert - 1
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"Falha! A API retornou {response.StatusCode}. Detalhe do Erro: {responseContent}");

        // Assert - 2
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entries = await dbContext.Entries.ToListAsync();
        var outboxMessages = await dbContext.OutboxMessages.ToListAsync();

        Assert.Single(entries);
        Assert.Equal("Pagamento da internet", entries[0].Description);
        Assert.Equal(150.50m, entries[0].Amount);

        Assert.Single(outboxMessages);
        Assert.Equal("entry-events", outboxMessages[0].Topic);
        Assert.Null(outboxMessages[0].ProcessedAt);
        Assert.Contains(entries[0].Id.ToString(), outboxMessages[0].Payload);
    }

    [Fact]
    public async Task PostEntry_WhenPayloadIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        CreateEntryCommand? nullCommand = null;

        // Act
        var response = await client.PostAsJsonAsync("/api/entry", nullCommand);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
