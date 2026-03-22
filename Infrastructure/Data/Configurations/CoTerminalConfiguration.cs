using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class CoTerminalConfiguration : BaseEntityConfiguration<CoTerminal, int>
    {
        public override void Configure(EntityTypeBuilder<CoTerminal> builder)
        {
            base.Configure(builder);

            builder.ToTable("CoTerminal");

            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.PostgraduateProgramName).HasColumnName("PostgraduateProgramName").HasMaxLength(255).IsRequired(false);
            builder.Property(e => e.UniversityName).HasColumnName("UniversityName").HasMaxLength(255).HasDefaultValue("Universidad Popular del Cesar").IsRequired(false);
            builder.Property(e => e.FirstSemesterAverage).HasColumnName("FirstSemesterAverage").HasColumnType("numeric(3,2)").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text").IsRequired(false);

            // Relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithOne(im => im.CoTerminal)
                .HasForeignKey<CoTerminal>(e => e.Id);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
