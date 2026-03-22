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

            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.ExamDate).HasColumnName("ExamDate").IsRequired(false);
            builder.Property(e => e.ResultQuintile).HasColumnName("ResultQuintile").HasMaxLength(50).IsRequired(false);
            builder.Property(e => e.ResultScore).HasColumnName("ResultScore").HasColumnType("numeric(10,2)").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text").IsRequired(false);

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
