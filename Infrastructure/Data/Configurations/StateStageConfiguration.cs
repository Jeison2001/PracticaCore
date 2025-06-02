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
            builder.Property(e => e.IdStageModality).IsRequired().HasColumnName("idstagemodality");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IsInitialState).IsRequired().HasColumnName("isinitialstate").HasDefaultValue(false);
            builder.Property(e => e.IsFinalStateForStage).IsRequired().HasColumnName("isfinalstateforstage").HasDefaultValue(false);
            builder.Property(e => e.IsFinalStateForModalityOverall).IsRequired().HasColumnName("isfinalstateformodalityoverall").HasDefaultValue(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configure foreign key relationships
            builder.HasOne(e => e.StageModality)
                .WithMany(sm => sm.StateStages)
                .HasForeignKey(e => e.IdStageModality);
        }
    }
}
