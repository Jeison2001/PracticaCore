using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AcademicProgramConfiguration : BaseEntityConfiguration<AcademicProgram, int>
    {
        public override void Configure(EntityTypeBuilder<AcademicProgram> builder)
        {
            base.Configure(builder);
            builder.ToTable("AcademicProgram");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            // Configuración explícita de la relación con Faculty
            builder.HasOne(p => p.Faculty)
                    .WithMany(f => f.AcademicPrograms)
                    .HasForeignKey(p => p.IdFaculty);

            // Especificar el nombre exacto de la columna FK en la base de datos
            builder.Property(p => p.IdFaculty).HasColumnName("idfaculty");
        }
    }
}
