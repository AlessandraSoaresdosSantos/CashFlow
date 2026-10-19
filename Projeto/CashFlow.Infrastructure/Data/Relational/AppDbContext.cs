using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Entry> Entries { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

          modelBuilder.Entity<Entry>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2) 
            .IsRequired();       

        modelBuilder.Entity<Entry>()
            .Property(e => e.Description)
            .HasMaxLength(250)
            .IsRequired();

        modelBuilder.Entity<OutboxMessage>()
            .Property(o => o.Topic)
            .HasMaxLength(100)
            .IsRequired();
    }
}