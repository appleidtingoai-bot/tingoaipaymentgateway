using Microsoft.EntityFrameworkCore;
using TingoAI.PaymentGateway.Domain.Entities;

namespace TingoAI.PaymentGateway.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.MerchantTransactionReference)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.GlobalPayTransactionReference)
                .HasMaxLength(200);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.CustomerFirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CustomerLastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CustomerEmail)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.CustomerAddress)
                .HasMaxLength(500);

            entity.Property(e => e.PaymentStatus)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.CheckoutUrl)
                .HasMaxLength(1000);

            entity.Property(e => e.AccessCode)
                .HasMaxLength(100);

            entity.Property(e => e.PaymentChannel)
                .HasMaxLength(50);

            entity.Property(e => e.ResponseCode)
                .HasMaxLength(50);

            entity.Property(e => e.ResponseMessage)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes for performance
            entity.HasIndex(e => e.MerchantTransactionReference)
                .IsUnique();

            entity.HasIndex(e => e.GlobalPayTransactionReference);

            entity.HasIndex(e => e.Currency);

            entity.HasIndex(e => e.PaymentStatus);

            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
