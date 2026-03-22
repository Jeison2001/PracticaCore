using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ProjectFinalConfiguration : BaseEntityConfiguration<ProjectFinal, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectFinal> builder)
        {            base.Configure(builder);
            builder.ToTable("ProjectFinal");
            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.ReportApprovalDate).HasColumnName("ReportApprovalDate");
            builder.Property(e => e.FinalPhaseApprovalDate).HasColumnName("FinalPhaseApprovalDate");
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
