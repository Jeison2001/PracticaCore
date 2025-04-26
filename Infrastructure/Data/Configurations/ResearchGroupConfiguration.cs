using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ResearchGroupConfiguration : BaseEntityConfiguration<ResearchGroup, int>
    {
        public override void Configure(EntityTypeBuilder<ResearchGroup> builder)
        {
            base.Configure(builder);
            builder.ToTable("ResearchGroup");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}