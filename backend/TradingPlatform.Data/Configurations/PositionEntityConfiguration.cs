using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class PositionEntityConfiguration : IEntityTypeConfiguration<PositionEntity>
{
    public void Configure(EntityTypeBuilder<PositionEntity> builder)
    {
        builder.ToTable("Positions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Positions_Quantity_NonNegative", "[Quantity] >= 0 AND [ReservedQuantity] >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.ReservedQuantity)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.AverageOpenPrice)
            .HasColumnType("decimal(18,8)");

        builder.Property(x => x.OpenedAtUtc)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.LastUpdateTime)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => new { x.AccountId, x.InstrumentId })
            .IsUnique();

        builder.HasOne(x => x.Instrument)
            .WithMany(x => x.Positions)
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Positions)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
    }
}
