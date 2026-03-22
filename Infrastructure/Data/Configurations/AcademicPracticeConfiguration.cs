using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AcademicPracticeConfiguration : BaseEntityConfiguration<AcademicPractice, int>
    {
        public override void Configure(EntityTypeBuilder<AcademicPractice> builder)
        {
            base.Configure(builder);
            
            builder.ToTable("AcademicPractice");

            // Required fields
            builder.Property(e => e.IdStateStage)
                .HasColumnName("IdStateStage")
                .IsRequired();

            // Title field
            builder.Property(e => e.Title)
                .HasColumnName("Title")
                .HasMaxLength(500)
                .HasDefaultValue("")
                .IsRequired();

            // Institution fields
            builder.Property(e => e.InstitutionName)
                .HasColumnName("InstitutionName")
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(e => e.InstitutionContact)
                .HasColumnName("InstitutionContact")
                .HasMaxLength(250)
                .IsRequired(false);

            // Practice dates
            builder.Property(e => e.PracticeStartDate)
                .HasColumnName("PracticeStartDate")
                .IsRequired(false);

            builder.Property(e => e.PracticeEndDate)
                .HasColumnName("PracticeEndDate")
                .IsRequired(false);

            // Emprendimiento flag
            builder.Property(e => e.IsEmprendimiento)
                .HasColumnName("IsEmprendimiento")
                .HasDefaultValue(false);

            // General observations
            builder.Property(e => e.Observations)
                .HasColumnName("Observations")
                .HasColumnType("text")
                .IsRequired(false);

            // Phase-specific approval dates
            builder.Property(e => e.AvalApprovalDate)
                .HasColumnName("AvalApprovalDate")
                .IsRequired(false);

            builder.Property(e => e.PlanApprovalDate)
                .HasColumnName("PlanApprovalDate")
                .IsRequired(false);

            builder.Property(e => e.DevelopmentCompletionDate)
                .HasColumnName("DevelopmentCompletionDate")
                .IsRequired(false);

            builder.Property(e => e.FinalReportApprovalDate)
                .HasColumnName("FinalReportApprovalDate")
                .IsRequired(false);

            builder.Property(e => e.FinalApprovalDate)
                .HasColumnName("FinalApprovalDate")
                .IsRequired(false);

            // Practice management fields
            builder.Property(e => e.PracticeHours)
                .HasColumnName("PracticeHours")
                .HasDefaultValue(640)
                .IsRequired(false);

            builder.Property(e => e.EvaluatorObservations)
                .HasColumnName("EvaluatorObservations")
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(e => e.IdUserCreatedAt)
                .HasColumnName("IdUserCreatedAt")
                .IsRequired(false);

            // Configure relationships
            builder.HasOne(ap => ap.InscriptionModality)
                .WithOne(im => im.AcademicPractice)
                .HasForeignKey<AcademicPractice>(ap => ap.Id);

            builder.HasOne(ap => ap.StateStage)
                .WithMany()
                .HasForeignKey(ap => ap.IdStateStage);
        }
    }
}
