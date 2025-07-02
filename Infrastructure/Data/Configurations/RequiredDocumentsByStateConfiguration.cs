using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RequiredDocumentsByStateConfiguration : BaseEntityConfiguration<RequiredDocumentsByState, int>
    {
        public override void Configure(EntityTypeBuilder<RequiredDocumentsByState> builder)
        {
            base.Configure(builder);
            
            builder.ToTable("RequiredDocumentsByState");
            
            // Primary key
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();
            
            // Required fields
            builder.Property(e => e.IdStateStage)
                .HasColumnName("idstatestage")
                .IsRequired();
                
            builder.Property(e => e.IdDocumentType)
                .HasColumnName("iddocumenttype")
                .IsRequired();
            
            builder.Property(e => e.IsRequired)
                .HasColumnName("isrequired")
                .HasDefaultValue(true);
                
            builder.Property(e => e.OrderDisplay)
                .HasColumnName("orderdisplay")
                .IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Unique constraint
            builder.HasIndex(e => new { e.IdStateStage, e.IdDocumentType })
                .HasDatabaseName("UQ_RequiredDocState")
                .IsUnique();
            
            // Foreign key relationships
            builder.HasOne(rds => rds.StateStage)
                .WithMany()
                .HasForeignKey(rds => rds.IdStateStage)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(rds => rds.DocumentType)
                .WithMany()
                .HasForeignKey(rds => rds.IdDocumentType)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
