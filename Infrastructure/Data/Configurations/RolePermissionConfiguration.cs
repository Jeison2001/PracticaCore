using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RolePermissionConfiguration : BaseEntityConfiguration<RolePermission, int>
    {
        public override void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            base.Configure(builder);
            builder.ToTable("RolePermission");
            builder.Property(rp => rp.IdRole).IsRequired().HasColumnName("idrole");
            builder.Property(rp => rp.IdPermission).IsRequired().HasColumnName("idpermission");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración explícita de la relación con Role
            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.IdRole);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.IdPermission);

            builder.HasIndex(rp => new { rp.IdRole, rp.IdPermission }).IsUnique();
        }
    }
}