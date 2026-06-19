using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class CryptoTransactionEntityConfiguration : IEntityTypeConfiguration<CryptoTransactionEntity>
{
    public void Configure(EntityTypeBuilder<CryptoTransactionEntity> builder)
    {
        builder.ToTable("CryptoTransactions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_CryptoTransactions_Quantity_NonNegative", "[Quantity] > 0");
            tableBuilder.HasCheckConstraint("CK_CryptoTransactions_Prices_NonNegative", "[RequestedPrice] > 0 AND [ExecutionPrice] > 0 AND [TotalValue] >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.RequestedPrice)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.ExecutionPrice)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.TotalValue)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.ErrorReason)
            .HasMaxLength(500);

        // Indexes for efficient queries
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt })
            .HasDatabaseName("IX_CryptoTransactions_AccountId_CreatedAt");

        builder.HasIndex(x => new { x.AccountId, x.Symbol })
            .HasDatabaseName("IX_CryptoTransactions_AccountId_Symbol");

        builder.HasIndex(x => new { x.AccountId, x.Status })
            .HasDatabaseName("IX_CryptoTransactions_AccountId_Status");

        builder.HasIndex(x => new { x.AccountId, x.Type })
            .HasDatabaseName("IX_CryptoTransactions_AccountId_Type");

        // Foreign keys - Restrict to protect ledger history
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict); // Never delete account history

        builder.HasOne(x => x.Instrument)
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
    }
}
