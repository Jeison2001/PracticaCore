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
            builder.Property(e => e.IdModality).IsRequired().HasColumnName("idmodality");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.StageOrder).IsRequired().HasColumnName("stageorder");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

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
