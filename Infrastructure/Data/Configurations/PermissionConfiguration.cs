using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class PermissionConfiguration : BaseEntityConfiguration<Permission, int>
    {
        public override void Configure(EntityTypeBuilder<Permission> builder)
        {
            base.Configure(builder);
            builder.ToTable("Permission");
            builder.Property(p => p.Code).IsRequired().HasMaxLength(10).HasColumnName("code");
            builder.Property(p => p.ParentCode).IsRequired().HasMaxLength(10).HasColumnName("parentcode").IsRequired(false);
            builder.Property(p => p.Description).IsRequired().HasMaxLength(255).HasColumnName("description");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración de la relación jerárquica
            builder.HasOne(p => p.ParentPermission)
                   .WithMany(p => p.ChildPermissions)
                   .HasForeignKey(p => p.ParentCode)
                   .HasPrincipalKey(p => p.Code)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}