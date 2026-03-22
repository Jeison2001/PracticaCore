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

            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.CertifiedAverage).HasColumnName("CertifiedAverage").HasColumnType("numeric(3,2)").IsRequired(false);
            builder.Property(e => e.HasFailedSubjects).HasColumnName("HasFailedSubjects").HasDefaultValue(false).IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text").IsRequired(false);

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
