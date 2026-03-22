using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ThematicAreaConfiguration : BaseEntityConfiguration<ThematicArea, int>
    {
        public override void Configure(EntityTypeBuilder<ThematicArea> builder)
        {
            base.Configure(builder);
            builder.ToTable("ThematicArea");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(150).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdResearchSubLine).HasColumnName("IdResearchSubLine");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configure relationship with ResearchSubLine
            builder.HasOne(t => t.ResearchSubLine)
                   .WithMany(rs => rs.ThematicAreas)
                   .HasForeignKey(t => t.IdResearchSubLine);
        }
    }
}