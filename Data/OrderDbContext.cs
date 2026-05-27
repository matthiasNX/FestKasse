using FestKasse.Models;
using Microsoft.EntityFrameworkCore;

namespace FestKasse.Data;

public class OrderDbContext : DbContext
{
    public DbSet<OrderRecord> Orders { get; set; } = null!;
    public DbSet<OrderItemRecord> OrderItems { get; set; } = null!;

    private readonly string _dbPath;

    public OrderDbContext()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_orders.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderRecord>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Total).HasColumnType("TEXT"); // SQLite stores decimals as TEXT for precision
            e.HasMany(o => o.Items)
             .WithOne()
             .HasForeignKey(i => i.OrderRecordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItemRecord>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("TEXT");
            e.Property(i => i.LineTotal).HasColumnType("TEXT");
        });
    }
}
