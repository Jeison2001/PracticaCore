using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserInscriptionModalityConfiguration : BaseEntityConfiguration<UserInscriptionModality, int>
    {
        public override void Configure(EntityTypeBuilder<UserInscriptionModality> builder)
        {
            base.Configure(builder);
            builder.ToTable("UserInscriptionModality");
            
            builder.Property(e => e.IdInscriptionModality).IsRequired().HasColumnName("IdInscriptionModality");
            builder.Property(e => e.IdUser).IsRequired().HasColumnName("IdUser");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configuración de relaciones
            builder.HasOne(rms => rms.InscriptionModality)
                .WithMany()
                .HasForeignKey(rms => rms.IdInscriptionModality);

            builder.HasOne(rms => rms.User)
                .WithMany()
                .HasForeignKey(rms => rms.IdUser);
                
            // Definir unicidad para la relación
            builder.HasIndex(rms => new { rms.IdInscriptionModality, rms.IdUser }).IsUnique();
        }
    }
}