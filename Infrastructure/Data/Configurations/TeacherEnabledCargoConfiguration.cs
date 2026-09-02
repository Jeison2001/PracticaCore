using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TeacherEnabledCargoConfiguration : BaseEntityConfiguration<TeacherEnabledCargo, int>
    {
        public override void Configure(EntityTypeBuilder<TeacherEnabledCargo> builder)
        {
            base.Configure(builder);
            builder.ToTable("TeacherEnabledCargo");
            builder.Property(e => e.IdUser).HasColumnName("IdUser");
            builder.Property(e => e.IdTypeTeachingAssignment).HasColumnName("IdTypeTeachingAssignment");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Relación con User (docente)
            builder.HasOne(e => e.User)
                   .WithMany(u => u.TeacherEnabledCargos)
                   .HasForeignKey(e => e.IdUser);

            // Relación con TypeTeachingAssignment (cargo)
            builder.HasOne(e => e.TypeTeachingAssignment)
                   .WithMany(t => t.TeacherEnabledCargos)
                   .HasForeignKey(e => e.IdTypeTeachingAssignment);

            // Un docente no repite cargo habilitado
            builder.HasIndex(e => new { e.IdUser, e.IdTypeTeachingAssignment }).IsUnique();
        }
    }
}
