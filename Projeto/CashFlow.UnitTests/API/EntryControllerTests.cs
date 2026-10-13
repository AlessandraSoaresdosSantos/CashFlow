using System;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Events;
using CashFlow.EntryAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CashFlow.UnitTests.API;

public class EntryControllerTests
{
    private readonly Mock<IEntryRepository> _mockRepo;
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<EntryController>> _mockLogger;
    private readonly CreateEntryCommandHandler _handler;
    private readonly EntryController _controller;

    public EntryControllerTests()
    {
        // 1. Inicializa os Mocks das dependências externas
        _mockRepo = new Mock<IEntryRepository>();
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<EntryController>>();

        // 2. Instancia o Handler injetando os mocks
        _handler = new CreateEntryCommandHandler(_mockRepo.Object, _mockBus.Object);

        // 3. Instancia o Controller injetando o Handler e o Logger falsos
        _controller = new EntryController(_handler, _mockLogger.Object);
    }

    [Fact]
    public async Task Post_ValidCommand_ShouldReturnCreated()
    {
        // Arrange: Prepara um lançamento válido
        var command = new CreateEntryCommand("Venda de Pudim", 150.00m, 1);

        // Act: Executa o endpoint
        var result = await _controller.Post(command);

        // Assert: Valida se retornou HTTP 201 Created
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);

        // Verifica se as rotinas de banco e mensageria foram acionadas
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Entry>()), Times.Once);
        _mockBus.Verify(b => b.PublishAsync("entry-events", It.IsAny<EntryCreatedEvent>()), Times.Once);
    }

    [Theory]
    [InlineData("", 100)]   // Descrição em branco
    [InlineData("Venda", 0)] // Valor zero
    [InlineData("Venda", -10)] // Valor negativo
    public async Task Post_InvalidInput_ShouldReturnBadRequest(string description, decimal amount)
    {
        // Arrange: Prepara comandos com dados inválidos que falham na validação do Controller
        var command = new CreateEntryCommand(description, amount, 1);

        // Act
        var result = await _controller.Post(command);

        // Assert: Valida se retornou HTTP 400 Bad Request
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        // Garante que o banco de dados NÃO foi chamado
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Entry>()), Times.Never);
    }

    [Fact]
    public async Task Post_DomainError_ShouldReturnBadRequest()
    {
        // Arrange: Passamos um Type inválido (ex: 99) para passar pela validação da API, 
        // mas forçar um disparo de ArgumentException dentro da regra de negócio (Domain)
        var command = new CreateEntryCommand("Venda de Pudim", 100.00m, 99);

        // Act
        var result = await _controller.Post(command);

        // Assert: O bloco catch (ArgumentException) deve capturar e retornar 400
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Post_RepositoryThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange: Simula uma queda catastrófica no SQL Server
        var command = new CreateEntryCommand("Venda de Pudim", 100.00m, 1);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Entry>()))
                 .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.Post(command);

        // Assert: O bloco catch (Exception) deve capturar e retornar 500
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}