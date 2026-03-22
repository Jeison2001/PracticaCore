using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TeachingAssignmentConfiguration : BaseEntityConfiguration<TeachingAssignment, int>
    {
        public override void Configure(EntityTypeBuilder<TeachingAssignment> builder)
        {
            base.Configure(builder);
            builder.ToTable("TeachingAssignment");
            builder.Property(e => e.IdInscriptionModality).IsRequired().HasColumnName("IdInscriptionModality");
            builder.Property(e => e.IdTeacher).IsRequired().HasColumnName("IdTeacher");
            builder.Property(e => e.IdTypeTeachingAssignment).IsRequired().HasColumnName("IdTypeTeachingAssignment");
            builder.Property(e => e.RevocationDate).HasColumnName("RevocationDate").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
            builder.Property(e => e.IdTeacherResearchProfile).HasColumnName("IdTeacherResearchProfile").IsRequired(false);

            // Configure foreign key relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithMany(t => t.TeachingAssignments)
                .HasForeignKey(e => e.IdInscriptionModality);
                
            builder.HasOne(e => e.Teacher)
                .WithMany(t => t.TeachingAssignments)
                .HasForeignKey(e => e.IdTeacher);
                
            builder.HasOne(e => e.TypeTeachingAssignment)
                .WithMany(t => t.TeachingAssignments)
                .HasForeignKey(e => e.IdTypeTeachingAssignment);

            builder.HasOne(e => e.TeacherResearchProfile)
                .WithMany(p => p.TeachingAssignments)
                .HasForeignKey(e => e.IdTeacherResearchProfile);
        }
    }
}