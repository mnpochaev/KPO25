using Gozon.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gozon.Payments.Data;

public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.UserId);

            e.Property(x => x.Balance).HasPrecision(18, 2).IsRequired();
        });

        modelBuilder.Entity<InboxMessage>(e =>
        {
            e.ToTable("inbox_messages");
            e.HasKey(x => x.MessageId);

            e.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);

            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.Property(x => x.PayloadJson).IsRequired();
            e.Property(x => x.OccurredAtUtc).IsRequired();
            e.Property(x => x.Attempts).IsRequired();
            e.HasIndex(x => x.PublishedAtUtc);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.OrderId); // уникальность по заказу

            e.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasIndex(x => x.UserId);
        });
    }
}