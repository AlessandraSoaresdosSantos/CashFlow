using System.Text.Json;
using Moq;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;

namespace CashFlow.UnitTests.Handlers;

public class CreateEntryCommandHandlerTests
{
    private readonly Mock<IEntryRepository> _mockRepository;
    private readonly CreateEntryCommandHandler _handler;

    public CreateEntryCommandHandlerTests()
    {
        _mockRepository = new Mock<IEntryRepository>();
        _handler = new CreateEntryCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ShouldReturnGeneratedGuid()
    {
        // Arrange
        var command = new CreateEntryCommand
        {
            Description = "Compra de suprimentos",
            Amount = 250.75m,
            Type = EntryType.Debit
        };

        // Act
        var resultId = await _handler.Handle(command);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);

        _mockRepository.Verify(r => r.AddWithOutboxAsync(It.IsAny<Entry>(), It.IsAny<OutboxMessage>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExecuted_ShouldMapEntryAndOutboxCorrectly()
    {
        // Arrange
        var command = new CreateEntryCommand
        {
            Description = "Venda de serviço de consultoria",
            Amount = 1500.00m,
            Type = EntryType.Credit
        };

        Entry? capturedEntry = null;
        OutboxMessage? capturedOutbox = null;

        _mockRepository
            .Setup(r => r.AddWithOutboxAsync(It.IsAny<Entry>(), It.IsAny<OutboxMessage>()))
            .Callback<Entry, OutboxMessage>((entry, outbox) =>
            {
                capturedEntry = entry;
                capturedOutbox = outbox;
            })
            .Returns(Task.CompletedTask);

        // Act
        var resultId = await _handler.Handle(command);

        // Assert - 1. Valida se a entidade de Domínio (Entry) foi gerada corretamente
        Assert.NotNull(capturedEntry);
        Assert.Equal(resultId, capturedEntry.Id); // O ID retornado deve ser exatamente o gerado
        Assert.Equal(command.Description, capturedEntry.Description);
        Assert.Equal(command.Amount, capturedEntry.Amount);
        Assert.Equal(command.Type, capturedEntry.Type);

        // Assert - 2. Valida se a mensagem do Outbox foi estruturada corretamente
        Assert.NotNull(capturedOutbox);
        Assert.Equal("entry-events", capturedOutbox.Topic);
        Assert.Null(capturedOutbox.ProcessedAt); // A mensagem acabou de ser criada, logo não foi processada
        Assert.Null(capturedOutbox.Error);

        // Assert - 3. Valida se o Payload JSON guarda a fotografia exata do Evento
        var eventPayload = JsonSerializer.Deserialize<EntryCreatedEvent>(capturedOutbox.Payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(eventPayload);
        Assert.Equal(capturedEntry.Id, eventPayload.Id);
        Assert.Equal(capturedEntry.Amount, eventPayload.Amount);
        Assert.Equal(capturedEntry.Type, eventPayload.Type);
        Assert.Equal(capturedEntry.CreatedAt, eventPayload.CreatedAt);
    }
}