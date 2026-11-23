using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class EmailAlertDispatchLogConfiguration : BaseEntityConfiguration<EmailAlertDispatchLog, int>
    {
        public override void Configure(EntityTypeBuilder<EmailAlertDispatchLog> builder)
        {
            base.Configure(builder);

            builder.ToTable("EmailAlertDispatchLog");

            builder.Property(x => x.IdInscriptionModality)
                .HasColumnName("IdInscriptionModality")
                .IsRequired();

            builder.Property(x => x.AlertCode)
                .HasColumnName("AlertCode")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EventName)
                .HasColumnName("EventName")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.StageStateSnapshot)
                .HasColumnName("StageStateSnapshot")
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.DeduplicationKey)
                .HasColumnName("DeduplicationKey")
                .HasMaxLength(400)
                .IsRequired();

            builder.Property(x => x.RecipientsSummary)
                .HasColumnName("RecipientsSummary")
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(x => x.BusinessDaysElapsed)
                .HasColumnName("BusinessDaysElapsed")
                .IsRequired();

            builder.HasIndex(x => x.DeduplicationKey)
                .IsUnique()
                .HasDatabaseName("IX_EmailAlertDispatchLog_DeduplicationKey");

            builder.HasIndex(x => x.IdInscriptionModality)
                .HasDatabaseName("IX_EmailAlertDispatchLog_IdInscriptionModality");

            builder.HasOne(x => x.InscriptionModality)
                .WithMany()
                .HasForeignKey(x => x.IdInscriptionModality)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
