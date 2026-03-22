using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ResearchSubLineConfiguration : BaseEntityConfiguration<ResearchSubLine, int>
    {
        public override void Configure(EntityTypeBuilder<ResearchSubLine> builder)
        {
            base.Configure(builder);
            builder.ToTable("ResearchSubLine");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdResearchLine).HasColumnName("IdResearchLine");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configure relationship with ResearchLine
            builder.HasOne(r => r.ResearchLine)
                   .WithMany(rl => rl.ResearchSubLines)
                   .HasForeignKey(r => r.IdResearchLine);
        }
    }
}