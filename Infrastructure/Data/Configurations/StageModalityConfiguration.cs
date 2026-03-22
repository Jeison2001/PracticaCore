using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class StageModalityConfiguration : BaseEntityConfiguration<StageModality, int>
    {
        public override void Configure(EntityTypeBuilder<StageModality> builder)
        {
            base.Configure(builder);
            builder.ToTable("StageModality");
            builder.Property(e => e.IdModality).IsRequired().HasColumnName("IdModality");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.StageOrder).IsRequired().HasColumnName("StageOrder");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configure foreign key relationships
            builder.HasOne(e => e.Modality)
                .WithMany()
                .HasForeignKey(e => e.IdModality);

            // Configure relationship with StateStages
            builder.HasMany(e => e.StateStages)
                .WithOne(ss => ss.StageModality)
                .HasForeignKey(ss => ss.IdStageModality);
        }
    }
}
