using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class DocumentTypeConfiguration : BaseEntityConfiguration<DocumentType, int>
    {
        public override void Configure(EntityTypeBuilder<DocumentType> builder)
        {
            base.Configure(builder);
            builder.ToTable("DocumentType");
              builder.Property(e => e.IdDocumentClass).HasColumnName("IdDocumentClass").IsRequired();
            builder.Property(e => e.IdStageModality).HasColumnName("IdStageModality").IsRequired(false);
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configure relationships            
            builder.HasOne(e => e.DocumentClass)
                .WithMany(dc => dc.DocumentTypes)
                .HasForeignKey(e => e.IdDocumentClass);

            builder.HasOne(e => e.StageModality)
                .WithMany()
                .HasForeignKey(e => e.IdStageModality)
                .IsRequired(false);
        }
    }
}