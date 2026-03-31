using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class AccountTransferEntityConfiguration : IEntityTypeConfiguration<AccountTransferEntity>
{
    public void Configure(EntityTypeBuilder<AccountTransferEntity> builder)
    {
        builder.ToTable("AccountTransfers", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_AccountTransfers_Amount_Positive", "[Amount] > 0");
            tableBuilder.HasCheckConstraint("CK_AccountTransfers_FromOrToRequired", "[FromAccountId] IS NOT NULL OR [ToAccountId] IS NOT NULL");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");
    }
}
