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
            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.ApprovalDate).HasColumnName("ApprovalDate");
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);

            // Relación 1:1 con InscriptionModality (patrón tabla extensión)
            builder.HasOne(e => e.InscriptionModality)
                .WithOne()
                .HasForeignKey<PreliminaryProject>(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
