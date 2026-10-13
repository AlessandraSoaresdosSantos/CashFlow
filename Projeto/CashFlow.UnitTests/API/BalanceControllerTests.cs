using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Queries;
using CashFlow.ConsolidationAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CashFlow.UnitTests.API;

public class BalanceControllerTests
{
    [Fact]
    public async Task Get_WhenCalledWithDate_ShouldReturnOkWithCorrectBalance()
    {
        // Arrange: Configuração dos dados de teste
        var dateRequested = "2026-10-12";
        var expectedBalance = 1450.75;

        // 1. Cria o Mock do Repositório (Simula o Redis)
        var mockRepository = new Mock<IBalanceRepository>();
        mockRepository
            .Setup(repo => repo.GetBalanceAsync(dateRequested))
            .ReturnsAsync(expectedBalance); // Retorna o valor falso quando a data for consultada

        // 2. Instancia o Handler real injetando o repositório falso
        var handler = new GetBalanceQueryHandler(mockRepository.Object);

        // 3. Instancia o Controller injetando o handler
        var controller = new BalanceController(handler);

        // Act: Executa o método GET da API
        var result = await controller.Get(dateRequested);

        // Assert: Validações

        // Verifica se o retorno HTTP foi 200 OK
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Como o controller devolve um objeto anônimo "new { Date = date, Balance = balance }",
        // usamos Reflection para ler os valores das propriedades dentro do projeto de testes
        var responseValue = okResult.Value;
        Assert.NotNull(responseValue);

        var dateProperty = responseValue.GetType().GetProperty("Date")?.GetValue(responseValue, null);
        var balanceProperty = responseValue.GetType().GetProperty("Balance")?.GetValue(responseValue, null);

        // Verifica se a API devolveu os dados corretos no JSON
        Assert.Equal(dateRequested, dateProperty);
        Assert.Equal(expectedBalance, (double)balanceProperty!);

        // Verifica se o repositório foi chamado exatamente 1 vez com a data certa
        mockRepository.Verify(repo => repo.GetBalanceAsync(dateRequested), Times.Once);
    }
}