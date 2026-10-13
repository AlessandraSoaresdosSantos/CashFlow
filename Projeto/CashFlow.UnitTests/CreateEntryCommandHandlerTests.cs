using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Events;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace CashFlow.UnitTests.Application;

public class CreateEntryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveOnRepositoryAndPublishEvent()
    {
        // Arrange: Configurar os mocks das interfaces externas
        var mockRepository = new Mock<IEntryRepository>();
        var mockMessageBus = new Mock<CashFlow.Application.Interfaces.IMessageBus>();

        var handler = new CreateEntryCommandHandler(mockRepository.Object, mockMessageBus.Object);
        var command = new CreateEntryCommand("Venda Diária", 250m, 1);

        // Act: Executar o caso de uso
        var resultId = await handler.Handle(command);

        // Assert: Garantir que a lógica de orquestração arquitetural foi seguida
        Assert.NotEqual(Guid.Empty, resultId);

        // Verifica se a gravação no SQL Server foi invocada exatamente uma vez
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Entry>()), Times.Once);
        mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);

        // Verifica se o evento foi enviado para o Kafka exatamente uma vez
        mockMessageBus.Verify(m => m.PublishAsync("entry-events", It.IsAny<EntryCreatedEvent>()), Times.Once);
    }
}