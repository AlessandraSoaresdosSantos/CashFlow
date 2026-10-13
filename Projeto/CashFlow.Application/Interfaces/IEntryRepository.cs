using System.Threading.Tasks;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Interfaces;

public interface IEntryRepository
{
    Task AddAsync(Entry entry);
    Task SaveChangesAsync();
}