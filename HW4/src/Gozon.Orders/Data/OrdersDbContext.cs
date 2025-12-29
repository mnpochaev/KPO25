using Gozon.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gozon.Orders.Data;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasIndex(x => x.UserId);
        });
        
        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => x.MessageId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);

            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.Property(x => x.PayloadJson).IsRequired();
            e.Property(x => x.OccurredAtUtc).IsRequired();
            e.Property(x => x.PublishedAtUtc);
            e.Property(x => x.Attempts).IsRequired();
            e.Property(x => x.LastError);

            // Чтобы быстро выбирать "неопубликованные"
            e.HasIndex(x => x.PublishedAtUtc);
        });
    }
}