using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;

namespace CashFlow.Infrastructure.Data.Relational;

public class EntryRepository : IEntryRepository
{
    private readonly AppDbContext _context;

    public EntryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Entry entry)
    {
        await _context.Entries.AddAsync(entry);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}