using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RegisterModalityStudentConfiguration : BaseEntityConfiguration<RegisterModalityStudent, long>
    {
        public override void Configure(EntityTypeBuilder<RegisterModalityStudent> builder)
        {
            base.Configure(builder);
            builder.ToTable("RegisterModalityStudent");
            
            builder.Property(e => e.IdRegisterModality).IsRequired().HasColumnName("idregistermodality");
            builder.Property(e => e.IdUser).IsRequired().HasColumnName("iduser");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración de relaciones
            builder.HasOne(rms => rms.RegisterModality)
                .WithMany()
                .HasForeignKey(rms => rms.IdRegisterModality);

            builder.HasOne(rms => rms.User)
                .WithMany()
                .HasForeignKey(rms => rms.IdUser);
                
            // Definir unicidad para la relación
            builder.HasIndex(rms => new { rms.IdRegisterModality, rms.IdUser }).IsUnique();
        }
    }
}