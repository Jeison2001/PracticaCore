using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ScientificArticleConfiguration : BaseEntityConfiguration<ScientificArticle, int>
    {
        public override void Configure(EntityTypeBuilder<ScientificArticle> builder)
        {
            base.Configure(builder);

            builder.ToTable("ScientificArticle");

            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.ArticleTitle).HasColumnName("ArticleTitle").HasMaxLength(500).IsRequired(false);
            builder.Property(e => e.JournalName).HasColumnName("JournalName").HasMaxLength(255).IsRequired(false);
            builder.Property(e => e.ISSN).HasColumnName("ISSN").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.JournalCategory).HasColumnName("JournalCategory").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.AcceptanceDate).HasColumnName("AcceptanceDate").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text").IsRequired(false);

            // Relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithOne(im => im.ScientificArticle)
                .HasForeignKey<ScientificArticle>(e => e.Id);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
