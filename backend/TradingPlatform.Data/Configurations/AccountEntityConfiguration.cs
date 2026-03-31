using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Accounts_Balances_NonNegative", "[AvailableBalance] >= 0 AND [ReservedBalance] >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(34)
            .IsRequired();

        builder.HasIndex(x => x.AccountNumber)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.AvailableBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ReservedBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(x => new { x.UserId, x.AccountType })
            .IsUnique()
            .HasFilter("[AccountType] = 1")
            .HasDatabaseName("IX_Accounts_OneMainAccountPerUser");

        builder.HasIndex(x => new { x.UserId, x.Pillar, x.ParentAccountId })
            .IsUnique()
            .HasFilter("[AccountType] = 2 AND [ParentAccountId] IS NOT NULL")
            .HasDatabaseName("IX_Accounts_OneSubaccountPerPillar");

        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.Subaccounts)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Positions)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.IncomingTransfers)
            .WithOne(x => x.ToAccount)
            .HasForeignKey(x => x.ToAccountId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.OutgoingTransfers)
            .WithOne(x => x.FromAccount)
            .HasForeignKey(x => x.FromAccountId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
