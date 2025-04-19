using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserPermissionConfiguration : BaseEntityConfiguration<UserPermission, int>
    {
        public override void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            base.Configure(builder);
            builder.ToTable("UserPermission");
            builder.Property(up => up.IdUser).IsRequired().HasColumnName("iduser");
            builder.Property(up => up.IdPermission).IsRequired().HasColumnName("idpermission");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración explícita de la relación con User
            builder.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.IdUser);

            // Configuración explícita de la relación con Permission
            builder.HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.IdPermission);

            // Constraint de unicidad
            builder.HasIndex(up => new { up.IdUser, up.IdPermission }).IsUnique();
        }
    }
}