using CashFlow.Domain.Entities;

namespace CashFlow.Application.Interfaces;

public interface IEntryRepository
{
    Task AddAsync(Entry entry);
    Task SaveChangesAsync();

    Task AddWithOutboxAsync(Entry entry, OutboxMessage outbox);
}