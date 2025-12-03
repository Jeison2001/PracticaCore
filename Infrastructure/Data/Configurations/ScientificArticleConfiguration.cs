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

            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage").IsRequired();
            builder.Property(e => e.ArticleTitle).HasColumnName("articletitle").HasMaxLength(500).IsRequired(false);
            builder.Property(e => e.JournalName).HasColumnName("journalname").HasMaxLength(255).IsRequired(false);
            builder.Property(e => e.ISSN).HasColumnName("issn").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.JournalCategory).HasColumnName("journalcategory").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.AcceptanceDate).HasColumnName("acceptancedate").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text").IsRequired(false);

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
