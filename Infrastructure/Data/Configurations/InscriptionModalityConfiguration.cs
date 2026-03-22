using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class InscriptionModalityConfiguration : BaseEntityConfiguration<InscriptionModality, int>
    {
        public override void Configure(EntityTypeBuilder<InscriptionModality> builder)
        {
            base.Configure(builder);
            builder.ToTable("InscriptionModality");

            builder.Property(e => e.IdModality).IsRequired().HasColumnName("IdModality");
            builder.Property(e => e.IdStateInscription).IsRequired().HasColumnName("IdStateInscription");
            builder.Property(e => e.IdAcademicPeriod).IsRequired().HasColumnName("IdAcademicPeriod");
            builder.Property(e => e.IdStageModality).HasColumnName("IdStageModality").IsRequired(false);
            builder.Property(e => e.ApprovalDate).HasColumnName("ApprovalDate").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configuración de relaciones
            builder.HasOne(rm => rm.Modality)
                .WithMany()
                .HasForeignKey(rm => rm.IdModality);

            builder.HasOne(rm => rm.StateInscription)
                .WithMany()
                .HasForeignKey(rm => rm.IdStateInscription);

            builder.HasOne(rm => rm.AcademicPeriod)
                .WithMany()
                .HasForeignKey(rm => rm.IdAcademicPeriod);
            builder.HasOne(rm => rm.StageModality)
                .WithMany(sm => sm.InscriptionModalities)
                .HasForeignKey(rm => rm.IdStageModality)
                .IsRequired(false);
        }
    }
}