using CashFlow.Domain.Enums;

namespace CashFlow.Application.UseCases.Commands;

public class CreateEntryCommand
{
    /// <summary>
    /// Descrição do lançamento (ex: "Pagamento de fornecedor", "Venda de produto")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Valor monetário do lançamento
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Tipo do lançamento: Crédito ou Débito
    /// </summary>
    public EntryType Type { get; set; }
}