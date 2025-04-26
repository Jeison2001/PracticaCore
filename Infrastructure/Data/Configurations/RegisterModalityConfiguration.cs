using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class RegisterModalityConfiguration : BaseEntityConfiguration<RegisterModality, int>
    {
        public override void Configure(EntityTypeBuilder<RegisterModality> builder)
        {
            base.Configure(builder);
            builder.ToTable("RegisterModality");
            
            builder.Property(e => e.IdModality).IsRequired().HasColumnName("idmodality");
            builder.Property(e => e.IdStateInscription).IsRequired().HasColumnName("idstateinscription");
            builder.Property(e => e.IdAcademicPeriod).IsRequired().HasColumnName("idacademicperiod");
            builder.Property(e => e.ApprovalDate).HasColumnName("approvaldate").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("observations").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

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
        }
    }
}