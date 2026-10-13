using System;
using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;


namespace CashFlow.Application.UseCases.Commands;

// O Input (DTO) que chega da API
public record CreateEntryCommand(string Description, decimal Amount, int Type);

// O orquestrador da regra de negócio (Application Service)
public class CreateEntryCommandHandler
{
    private readonly IEntryRepository _repository;
    private readonly IMessageBus _messageBus;

    public CreateEntryCommandHandler(IEntryRepository repository, IMessageBus messageBus)
    {
        _repository = repository;
        _messageBus = messageBus;
    }

    public async Task<Guid> Handle(CreateEntryCommand command)
    {
        // 1. Cria a entidade e executa as validações de domínio
        var entry = new Entry(command.Description, command.Amount, (EntryType)command.Type);

        // 2. Persiste os dados na base de dados ACID (SQL Server)
        await _repository.AddAsync(entry);
        await _repository.SaveChangesAsync();

        // 3. Publica o evento para o Kafka (A ser consumido pelo Worker de consolidação)
        var entryEvent = new EntryCreatedEvent(entry.Id, entry.Amount, (int)entry.Type, entry.CreatedAt);
        await _messageBus.PublishAsync("entry-events", entryEvent);

        return entry.Id;
    }
}