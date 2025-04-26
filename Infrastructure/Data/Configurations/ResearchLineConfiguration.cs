using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ResearchLineConfiguration : BaseEntityConfiguration<ResearchLine, int>
    {
        public override void Configure(EntityTypeBuilder<ResearchLine> builder)
        {
            base.Configure(builder);
            builder.ToTable("ResearchLine");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

        }
    }
}