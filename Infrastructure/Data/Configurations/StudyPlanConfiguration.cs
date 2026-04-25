using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class StudyPlanConfiguration : BaseEntityConfiguration<StudyPlan, int>
    {
        public override void Configure(EntityTypeBuilder<StudyPlan> builder)
        {
            base.Configure(builder);
            builder.ToTable("StudyPlan");

            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired(false).HasMaxLength(200).HasColumnName("Name");
            builder.Property(e => e.StartYear).IsRequired().HasColumnName("StartYear");
            builder.Property(e => e.EndYear).IsRequired(false).HasColumnName("EndYear");

            builder.HasOne(e => e.AcademicProgram)
                   .WithMany()
                   .HasForeignKey(e => e.IdAcademicProgram)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
