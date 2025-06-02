using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class PreliminaryProjectConfiguration : BaseEntityConfiguration<PreliminaryProject, int>
    {
        public override void Configure(EntityTypeBuilder<PreliminaryProject> builder)
        {            base.Configure(builder);
            builder.ToTable("PreliminaryProject");
            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage").IsRequired();
            builder.Property(e => e.ApprovalDate).HasColumnName("approvaldate");
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
