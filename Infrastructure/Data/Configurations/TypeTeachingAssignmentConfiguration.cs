using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TypeTeachingAssignmentConfiguration : BaseEntityConfiguration<TypeTeachingAssignment, int>
    {
        public override void Configure(EntityTypeBuilder<TypeTeachingAssignment> builder)
        {
            base.Configure(builder);
            builder.ToTable("TypeTeachingAssignment");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            
            // Add unique constraint for Code
            builder.HasIndex(e => e.Code).IsUnique();
        }
    }
}