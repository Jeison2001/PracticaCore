using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class StateStageConfiguration : BaseEntityConfiguration<StateStage, int>
    {
        public override void Configure(EntityTypeBuilder<StateStage> builder)
        {
            base.Configure(builder);
            builder.ToTable("StateStage");
            builder.Property(e => e.IdStageModality).IsRequired().HasColumnName("IdStageModality");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IsInitialState).IsRequired().HasColumnName("IsInitialState").HasDefaultValue(false);
            builder.Property(e => e.IsFinalStateForStage).IsRequired().HasColumnName("IsFinalStateForStage").HasDefaultValue(false);
            builder.Property(e => e.IsFinalStateForModalityOverall).IsRequired().HasColumnName("IsFinalStateForModalityOverall").HasDefaultValue(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configure foreign key relationships
            builder.HasOne(e => e.StageModality)
                .WithMany(sm => sm.StateStages)
                .HasForeignKey(e => e.IdStageModality);
        }
    }
}
