using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;

namespace CashFlow.UnitTests.Domain;

public class EntryTests
{
    [Fact]
    public void CreateEntry_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var description = "Venda de Pudim";
        var amount = 150m;
        var type = EntryType.Credit;

        // Act
        var entry = new Entry(description, amount, type);

        // Assert
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(description, entry.Description);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(type, entry.Type);
        Assert.True(entry.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50.5)]
    public void CreateEntry_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Entry("Teste de Falha", invalidAmount, EntryType.Debit));

        Assert.Equal("O valor do lançamento deve ser maior que zero.", exception.Message);
    }

    [Fact]
    public void CreateEntry_WithEmptyDescription_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Entry("", 100m, EntryType.Credit));

        Assert.Equal("A descrição do lançamento é obrigatória.", exception.Message);
    }
}