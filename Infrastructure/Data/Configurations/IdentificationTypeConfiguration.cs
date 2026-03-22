using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class IdentificationTypeConfiguration : BaseEntityConfiguration<IdentificationType, int>
    {
        public override void Configure(EntityTypeBuilder<IdentificationType> builder)
        {
            base.Configure(builder);
            builder.ToTable("IdentificationType");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}
