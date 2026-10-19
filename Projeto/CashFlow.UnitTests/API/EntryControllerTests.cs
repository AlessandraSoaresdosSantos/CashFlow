using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using CashFlow.EntryAPI.Controllers;
using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Domain.Enums;

namespace CashFlow.UnitTests.Controllers;

public class EntryControllerTests
{
    private readonly Mock<ICreateEntryCommandHandler> _mockHandler;
    private readonly EntryController _controller;

    public EntryControllerTests()
    {
       
        _mockHandler = new Mock<ICreateEntryCommandHandler>();

        _controller = new EntryController(_mockHandler.Object);
    }

    [Fact]
    public async Task Create_WhenCommandIsValid_ShouldReturnCreated201()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var command = new CreateEntryCommand
        {
            Description = "Recebimento de cliente",
            Amount = 500.00m,
            Type = EntryType.Credit
        };

        _mockHandler
            .Setup(h => h.Handle(It.IsAny<CreateEntryCommand>()))
            .ReturnsAsync(expectedId);

        // Act
        var result = await _controller.Create(command);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        Assert.NotNull(objectResult.Value);
        var valueProperty = objectResult.Value.GetType().GetProperty("Id");
        Assert.NotNull(valueProperty);

        var actualId = (Guid?)valueProperty.GetValue(objectResult.Value);
        Assert.Equal(expectedId, actualId);

        _mockHandler.Verify(h => h.Handle(command), Times.Once);
    }

    [Fact]
    public async Task Create_WhenCommandIsNull_ShouldReturnBadRequest400()
    {
        // Arrange
        CreateEntryCommand? nullCommand = null;

        // Act
        var result = await _controller.Create(nullCommand);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Equal("O payload do lançamento não pode ser nulo.", badRequestResult.Value);

        _mockHandler.Verify(h => h.Handle(It.IsAny<CreateEntryCommand>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenHandlerThrowsException_ShouldReturnInternalServerError500()
    {
        // Arrange
        var command = new CreateEntryCommand
        {
            Description = "Erro forçado",
            Amount = 10m,
            Type = EntryType.Debit
        };

        // Simulamos uma quebra interna (ex: Banco de dados fora do ar e falha no Outbox)
        _mockHandler
            .Setup(h => h.Handle(It.IsAny<CreateEntryCommand>()))
            .ThrowsAsync(new Exception("Falha de conexão com o banco de dados."));

        // Act
        var result = await _controller.Create(command);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        // Verifica se a resposta contém a mensagem de erro formatada
        Assert.NotNull(objectResult.Value);
        var errorProperty = objectResult.Value.GetType().GetProperty("Error");
        Assert.NotNull(errorProperty);

        var actualError = errorProperty.GetValue(objectResult.Value) as string;
        Assert.Equal("Erro interno ao processar o lançamento financeiro.", actualError);
    }
}