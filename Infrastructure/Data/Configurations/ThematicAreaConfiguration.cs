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
            builder.Property(e => e.Code).IsRequired().HasMaxLength(150).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdResearchSubLine).HasColumnName("idresearchsubline");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configure relationship with ResearchSubLine
            builder.HasOne(t => t.ResearchSubLine)
                   .WithMany(rs => rs.ThematicAreas)
                   .HasForeignKey(t => t.IdResearchSubLine);
        }
    }
}