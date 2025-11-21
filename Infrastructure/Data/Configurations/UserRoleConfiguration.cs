using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserRoleConfiguration : BaseEntityConfiguration<UserRole, int>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            base.Configure(builder);
            builder.ToTable("UserRole");
            builder.Property(e => e.IdUser).HasColumnName("iduser");
            builder.Property(e => e.IdRole).HasColumnName("idrole");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración explícita de la relación con User
            builder.HasOne(ur => ur.User)
                   .WithMany(ur => ur.UserRoles)
                   .HasForeignKey(ur => ur.IdUser);

            // Configuración explícita de la relación con Role
            builder.HasOne(ur => ur.Role)
                   .WithMany(ur => ur.UserRole)
                   .HasForeignKey(ur => ur.IdRole);
        }
    }
}
