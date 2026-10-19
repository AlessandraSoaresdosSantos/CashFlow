using CashFlow.Domain.Enums;
namespace CashFlow.Domain.Entities;
public class Entry
{
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public EntryType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    protected Entry() { }
    public Entry(string description, decimal amount, EntryType type)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição do lançamento é obrigatória.");

        if (amount <= 0)
            throw new ArgumentException("O valor do lançamento deve ser maior que zero.");

        if (!Enum.IsDefined(typeof(EntryType), type))
            throw new ArgumentException("Tipo de lançamento financeiro inválido.");

        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}