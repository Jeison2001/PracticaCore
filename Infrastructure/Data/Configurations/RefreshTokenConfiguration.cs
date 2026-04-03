using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken, long>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);
            builder.ToTable("RefreshTokens");

            builder.Property(e => e.IdUser).IsRequired().HasColumnName("IdUser");
            builder.Property(e => e.Token).IsRequired().HasMaxLength(200).HasColumnName("Token");
            builder.Property(e => e.ExpiresAt).IsRequired().HasColumnName("ExpiresAt");
            builder.Property(e => e.RevokedAt).HasColumnName("RevokedAt").IsRequired(false);

            builder.HasIndex(e => e.Token).IsUnique();

            builder.HasOne(rt => rt.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.IdUser);

            // Propiedades calculadas — no se mapean a columnas
            builder.Ignore(e => e.IsExpired);
            builder.Ignore(e => e.IsActive);
        }
    }
}
