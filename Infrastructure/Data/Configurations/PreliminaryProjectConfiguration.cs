using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class PreliminaryProjectConfiguration : BaseEntityConfiguration<PreliminaryProject, int>
    {
        public override void Configure(EntityTypeBuilder<PreliminaryProject> builder)
        {
            base.Configure(builder);
            builder.ToTable("PreliminaryProject");
            builder.Property(e => e.IdStatePreliminaryProject).HasColumnName("idstatepreliminaryproject").IsRequired();
            builder.Property(e => e.ApprovalDate).HasColumnName("approvaldate");
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat");
            builder.HasOne(e => e.StatePreliminaryProject)
                .WithMany()
                .HasForeignKey(e => e.IdStatePreliminaryProject);
        }
    }
}
