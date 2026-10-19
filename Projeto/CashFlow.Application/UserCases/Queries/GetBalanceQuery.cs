namespace CashFlow.Application.UseCases.Queries;

public class GetBalanceQuery
{
    /// <summary>
    /// A data (ou identificador) para a qual queremos consultar o saldo.
    /// Exemplo de formato esperado: "2026-10-18"
    /// </summary>
    public string Date { get; set; }

    public GetBalanceQuery()
    {
        Date = string.Empty;
    }

    public GetBalanceQuery(string date)
    {
        Date = date;
    }
}