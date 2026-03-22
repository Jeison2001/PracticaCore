using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TypeTeachingAssignmentConfiguration : BaseEntityConfiguration<TypeTeachingAssignment, int>
    {        public override void Configure(EntityTypeBuilder<TypeTeachingAssignment> builder)
        {
            base.Configure(builder);
            builder.ToTable("TypeTeachingAssignment");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.MaxAssignments).HasColumnName("MaxAssignments").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
            
            // Add unique constraint for Code
            builder.HasIndex(e => e.Code).IsUnique();
        }
    }
}