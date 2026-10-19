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
    public async Task Get_WhenCalledWithValidDate_ShouldReturnOkWithCorrectBalance()
    {
        // Arrange
        var dateRequested = "2026-10-12";
        var expectedBalance = 100.50m;

        var mockHandler = new Mock<IGetBalanceQueryHandler>();

        mockHandler
            .Setup(h => h.Handle(It.IsAny<GetBalanceQuery>()))
            .ReturnsAsync(expectedBalance);

        var controller = new BalanceController(mockHandler.Object);

        // Act
        var result = await controller.Get(dateRequested);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = okResult.Value;
        Assert.NotNull(responseValue);

        // CORREÇÃO: Utiliza reflexão segura em C# para ler propriedades de tipos anónimos públicos/internos
        var type = responseValue.GetType();
        var dateProperty = type.GetProperty("Date")?.GetValue(responseValue)?.ToString();
        var balanceProperty = type.GetProperty("Balance")?.GetValue(responseValue);

        Assert.Equal(dateRequested, dateProperty);
        Assert.NotNull(balanceProperty);
        Assert.Equal(expectedBalance, (decimal)balanceProperty);

        mockHandler.Verify(h => h.Handle(It.Is<GetBalanceQuery>(q => q.Date == dateRequested)), Times.Once);
    }

    [Theory]
    [InlineData("2026/10/12")] // Formato inválido com barras
    [InlineData("12-10-2026")] // Formato invertido (dd-MM-yyyy)
    [InlineData("data-invalida")]
    public async Task Get_WhenCalledWithInvalidDate_ShouldReturnBadRequest(string invalidDate)
    {
        var mockHandler = new Mock<IGetBalanceQueryHandler>();
        var controller = new BalanceController(mockHandler.Object);

        var result = await controller.Get(invalidDate);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        mockHandler.Verify(h => h.Handle(It.IsAny<GetBalanceQuery>()), Times.Never);
    }
}