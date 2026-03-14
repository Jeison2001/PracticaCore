using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AcademicAverageConfiguration : BaseEntityConfiguration<AcademicAverage, int>
    {
        public override void Configure(EntityTypeBuilder<AcademicAverage> builder)
        {
            base.Configure(builder);

            builder.ToTable("AcademicAverage");

            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage").IsRequired();
            builder.Property(e => e.CertifiedAverage).HasColumnName("certifiedaverage").HasColumnType("numeric(3,2)").IsRequired(false);
            builder.Property(e => e.HasFailedSubjects).HasColumnName("hasfailedsubjects").HasDefaultValue(false).IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text").IsRequired(false);

            // Relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithOne(im => im.AcademicAverage)
                .HasForeignKey<AcademicAverage>(e => e.Id);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
