using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Data.Relational;

public class AppDbContext : DbContext
{
    // O construtor tem de existir e receber as options!
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Entry> Entries { get; set; }
}