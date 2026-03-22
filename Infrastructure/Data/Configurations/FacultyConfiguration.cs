using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class FacultyConfiguration : BaseEntityConfiguration<Faculty, int>
    {
        public override void Configure(EntityTypeBuilder<Faculty> builder)
        {
            base.Configure(builder);
            builder.ToTable("Faculty");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("Code");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}
