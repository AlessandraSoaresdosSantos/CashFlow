using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Events;
public record EntryCreatedEvent(Guid Id, decimal Amount, EntryType Type, DateTime CreatedAt);
