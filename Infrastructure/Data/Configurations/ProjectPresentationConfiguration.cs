using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ProjectPresentationConfiguration : BaseEntityConfiguration<ProjectPresentation, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectPresentation> builder)
        {
            base.Configure(builder);
            builder.ToTable("ProjectPresentation");

            builder.Property(e => e.IdInscriptionModality).HasColumnName("IdInscriptionModality").IsRequired();
            builder.Property(e => e.IdStateStage).HasColumnName("IdStateStage").IsRequired();
            builder.Property(e => e.ScheduledDateTime).HasColumnName("ScheduledDateTime");
            builder.Property(e => e.ActualDateTime).HasColumnName("ActualDateTime");
            builder.Property(e => e.LocationOrLink).HasColumnName("LocationOrLink").HasMaxLength(255);
            builder.Property(e => e.PresentationModality).HasColumnName("PresentationModality").HasMaxLength(20);
            builder.Property(e => e.AttemptNumber).HasColumnName("AttemptNumber").HasDefaultValue(1).IsRequired();
            builder.Property(e => e.Observations).HasColumnName("Observations").HasColumnType("text");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);

            // Relación 1:1 con InscriptionModality (patrón tabla extensión)
            builder.HasOne(e => e.InscriptionModality)
                .WithOne()
                .HasForeignKey<ProjectPresentation>(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
