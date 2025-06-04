using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class DocumentClassConfiguration : BaseEntityConfiguration<DocumentClass, int>
    {
        public override void Configure(EntityTypeBuilder<DocumentClass> builder)
        {
            base.Configure(builder);
            builder.ToTable("DocumentClass");
            
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configure relationship with DocumentTypes
            builder.HasMany(e => e.DocumentTypes)
                .WithOne(dt => dt.DocumentClass)
                .HasForeignKey(dt => dt.IdDocumentClass);
        }
    }
}
