namespace CashFlow.Domain.Events;
public record EntryCreatedEvent(Guid Id, decimal Amount, int Type, DateTime CreatedAt);
