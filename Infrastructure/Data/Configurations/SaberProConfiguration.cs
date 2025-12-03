using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SaberProConfiguration : BaseEntityConfiguration<SaberPro, int>
    {
        public override void Configure(EntityTypeBuilder<SaberPro> builder)
        {
            base.Configure(builder);

            builder.ToTable("SaberPro");

            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage").IsRequired();
            builder.Property(e => e.ExamDate).HasColumnName("examdate").IsRequired(false);
            builder.Property(e => e.ResultQuintile).HasColumnName("resultquintile").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.ResultScore).HasColumnName("resultscore").HasColumnType("numeric(10,2)").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text").IsRequired(false);

            // Relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithOne(im => im.SaberPro)
                .HasForeignKey<SaberPro>(e => e.Id);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
