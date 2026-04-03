using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(Core.Enums.UserRole.User);

        builder.Property(x => x.TwoFactorSecret)
            .HasMaxLength(256);

        builder.Property(x => x.SecurityStamp)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasMany(x => x.Accounts)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
