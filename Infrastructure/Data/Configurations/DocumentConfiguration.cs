using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class DocumentConfiguration : BaseEntityConfiguration<Document, int>
    {
        public override void Configure(EntityTypeBuilder<Document> builder)
        {
            base.Configure(builder);
            builder.ToTable("Document");
            
            builder.Property(e => e.IdInscriptionModality).HasColumnName("idinscriptionmodality").IsRequired(false);
            builder.Property(e => e.IdUploader).HasColumnName("iduploader").IsRequired();
            builder.Property(e => e.IdDocumentType).HasColumnName("iddocumenttype").IsRequired();
            builder.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255).HasColumnName("originalfilename");
            builder.Property(e => e.StoredFileName).IsRequired().HasMaxLength(255).HasColumnName("storedfilename");
            builder.HasIndex(e => e.StoredFileName).IsUnique();
            builder.Property(e => e.StoragePath).IsRequired().HasMaxLength(1024).HasColumnName("storagepath");
            builder.Property(e => e.MimeType).IsRequired().HasMaxLength(100).HasColumnName("mimetype");
            builder.Property(e => e.FileSize).IsRequired().HasColumnName("filesize");
            builder.Property(e => e.Version).HasMaxLength(50).HasColumnName("version").IsRequired(false);
            builder.Property(e => e.DocumentState).IsRequired().HasMaxLength(50).HasColumnName("documentstate").HasDefaultValue("CARGADO");

            // Relationships
            builder.HasOne(d => d.DocumentType)
                .WithMany()
                .HasForeignKey(d => d.IdDocumentType);

            builder.HasOne(d => d.Uploader)
                .WithMany()
                .HasForeignKey(d => d.IdUploader);

            builder.HasOne(d => d.InscriptionModality)
                .WithMany()
                .HasForeignKey(d => d.IdInscriptionModality);
        }
    }
}