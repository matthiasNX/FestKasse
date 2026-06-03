using FestKasse.Models;
using Microsoft.EntityFrameworkCore;

namespace FestKasse.Data;

public class OrderDbContext : DbContext
{
    public DbSet<OrderRecord> Orders { get; set; } = null!;
    public DbSet<OrderItemRecord> OrderItems { get; set; } = null!;

    private static DbContextOptions<OrderDbContext>? _cachedOptions;

    private static DbContextOptions<OrderDbContext> GetOptions()
    {
        if (_cachedOptions != null) return _cachedOptions;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_orders.db");
        _cachedOptions = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return _cachedOptions;
    }

    public OrderDbContext() : base(GetOptions()) { }

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
