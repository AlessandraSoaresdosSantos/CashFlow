using CashFlow.Application.UseCases.Commands;

namespace CashFlow.Application.Interfaces;

public interface ICreateEntryCommandHandler
{
    Task<Guid> Handle(CreateEntryCommand command);
}