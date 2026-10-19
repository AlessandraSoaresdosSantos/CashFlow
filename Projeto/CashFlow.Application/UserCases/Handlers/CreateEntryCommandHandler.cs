using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Events;
using System.Text.Json;

public class CreateEntryCommandHandler : ICreateEntryCommandHandler
{
    private readonly IEntryRepository _repository;

    public CreateEntryCommandHandler(IEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateEntryCommand command)
    {
        var entry = new Entry(command.Description, command.Amount, command.Type);
        var eventPayload = new EntryCreatedEvent(entry.Id, entry.Amount, entry.Type, entry.CreatedAt);

        var outboxMessage = new OutboxMessage
        {
            Topic = "entry-events",
            Payload = JsonSerializer.Serialize(eventPayload)
        };

        await _repository.AddWithOutboxAsync(entry, outboxMessage);

        return entry.Id;
    }
}