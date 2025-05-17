using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ProjectFinalConfiguration : BaseEntityConfiguration<ProjectFinal, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectFinal> builder)
        {
            base.Configure(builder);
            builder.ToTable("ProjectFinal");
            builder.Property(e => e.IdStateProjectFinal).HasColumnName("idstateprojectfinal").IsRequired();
            builder.Property(e => e.ReportApprovalDate).HasColumnName("reportapprovaldate");
            builder.Property(e => e.FinalPhaseApprovalDate).HasColumnName("finalphaseapprovaldate");
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            builder.HasOne(e => e.StateProjectFinal)
                .WithMany()
                .HasForeignKey(e => e.IdStateProjectFinal);
        }
    }
}
