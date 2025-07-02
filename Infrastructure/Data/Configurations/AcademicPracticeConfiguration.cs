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
                .HasColumnName("idstatestage")
                .IsRequired();
                
            // Title field
            builder.Property(e => e.Title)
                .HasColumnName("title")                      // ✅ CORREGIDO: en minúscula
                .HasMaxLength(500)
                .HasDefaultValue("")
                .IsRequired();
            
            // Institution fields
            builder.Property(e => e.InstitutionName)
                .HasColumnName("institutionname")
                .HasMaxLength(500)
                .IsRequired(false);
            
            builder.Property(e => e.InstitutionContact)
                .HasColumnName("institutioncontact")
                .HasMaxLength(250)
                .IsRequired(false);
            
            // Practice dates
            builder.Property(e => e.PracticeStartDate)
                .HasColumnName("practicestartdate")
                .IsRequired(false);
                
            builder.Property(e => e.PracticeEndDate)
                .HasColumnName("practiceenddate")
                .IsRequired(false);
            
            // Emprendimiento flag
            builder.Property(e => e.IsEmprendimiento)
                .HasColumnName("isemprendimiento")
                .HasDefaultValue(false);
            
            // General observations
            builder.Property(e => e.Observations)
                .HasColumnName("observations")
                .HasColumnType("text")
                .IsRequired(false);
            
            // Phase-specific approval dates
            builder.Property(e => e.AvalApprovalDate)
                .HasColumnName("avalapprovaldate")
                .IsRequired(false);
                
            builder.Property(e => e.PlanApprovalDate)
                .HasColumnName("planapprovaldate")
                .IsRequired(false);
                
            builder.Property(e => e.DevelopmentCompletionDate)
                .HasColumnName("developmentcompletiondate")
                .IsRequired(false);
                
            builder.Property(e => e.FinalReportApprovalDate)
                .HasColumnName("finalreportapprovaldate")
                .IsRequired(false);
                
            builder.Property(e => e.FinalApprovalDate)
                .HasColumnName("finalapprovaldate")
                .IsRequired(false);
            
            // Practice management fields
            builder.Property(e => e.PracticeHours)
                .HasColumnName("practicehours")
                .HasDefaultValue(640)
                .IsRequired(false);
                
            builder.Property(e => e.EvaluatorObservations)
                .HasColumnName("evaluatorobservations")
                .HasColumnType("text")
                .IsRequired(false);
            
            builder.Property(e => e.IdUserCreatedAt)
                .HasColumnName("idusercreatedat")
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
