using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiChoice.MealMoney.Domain.Entities;
using MiChoice.MealMoney.Infrastructure.Identity;

namespace MiChoice.MealMoney.Infrastructure.Data;

public class MealMoneyDbContext : IdentityDbContext<MealMoneyUser>
{
    public MealMoneyDbContext(DbContextOptions<MealMoneyDbContext> options) : base(options) { }

    public DbSet<MealMoneyAccount>   MealMoneyAccounts   => Set<MealMoneyAccount>();
    public DbSet<HouseholdLink>      HouseholdLinks      => Set<HouseholdLink>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentMethod>      PaymentMethods      => Set<PaymentMethod>();

    // Sprint 8 — district admin refund tools
    public DbSet<RefundRequest>      RefundRequests      => Set<RefundRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MealMoneyAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Balance).HasPrecision(10, 2);
            e.Property(x => x.LowBalanceThreshold).HasPrecision(10, 2);
            e.HasMany(x => x.HouseholdLinks).WithOne(x => x.Account)
                .HasForeignKey(x => x.MealMoneyAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Transactions).WithOne(x => x.Account)
                .HasForeignKey(x => x.MealMoneyAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HouseholdLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ParentUserId, x.MealMoneyAccountId }).IsUnique();
        });

        builder.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(10, 2);
        });

        builder.Entity<PaymentMethod>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // ── RefundRequest ─────────────────────────────────────────────────────
        builder.Entity<RefundRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.RequestedBy).HasMaxLength(450).IsRequired();
            e.Property(x => x.RequestedByName).HasMaxLength(200);
            e.Property(x => x.ReviewedBy).HasMaxLength(200);
            e.Property(x => x.StripeRefundId).HasMaxLength(100);
            e.Property(x => x.DenialReason).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<int>();

            e.HasOne(x => x.Transaction)
             .WithMany()
             .HasForeignKey(x => x.PaymentTransactionId)
             .OnDelete(DeleteBehavior.Restrict); // preserve refund history if tx is deleted

            e.HasIndex(x => x.PaymentTransactionId); // look up refunds for a transaction
            e.HasIndex(x => x.Status);               // dashboard filter
        });
    }
}
