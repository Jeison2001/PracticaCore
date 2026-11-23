using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SlaAlertConfigurationConfiguration : BaseEntityConfiguration<SlaAlertConfiguration, int>
    {
        public override void Configure(EntityTypeBuilder<SlaAlertConfiguration> builder)
        {
            base.Configure(builder);

            builder.ToTable("SlaAlertConfiguration", t =>
            {
                t.HasCheckConstraint("CK_SlaAlertConfiguration_Singleton", "\"Id\" = 1");
                t.HasCheckConstraint("CK_SlaAlertConfiguration_ScheduleHour", "\"ScheduleHourColombia\" BETWEEN 0 AND 23");
                t.HasCheckConstraint("CK_SlaAlertConfiguration_ScheduleMinute", "\"ScheduleMinuteColombia\" BETWEEN 0 AND 59");
                t.HasCheckConstraint(
                    "CK_SlaAlertConfiguration_EvaluatorDays",
                    "\"EvaluatorWarningBusinessDays\" >= 1 AND \"EvaluatorOverdueBusinessDays\" <= 90 AND \"EvaluatorWarningBusinessDays\" < \"EvaluatorOverdueBusinessDays\"");
                t.HasCheckConstraint("CK_SlaAlertConfiguration_CommitteeDays", "\"CommitteeOverdueBusinessDays\" BETWEEN 1 AND 90");
                t.HasCheckConstraint("CK_SlaAlertConfiguration_StudentObsDays", "\"StudentObservationsBusinessDays\" BETWEEN 1 AND 90");
                t.HasCheckConstraint("CK_SlaAlertConfiguration_DefenseHours", "\"DefenseReminderHours\" BETWEEN 1 AND 168");
            });

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(x => x.IsEnabled)
                .HasColumnName("IsEnabled")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.ScheduleHourColombia)
                .HasColumnName("ScheduleHourColombia")
                .IsRequired()
                .HasDefaultValue(7);

            builder.Property(x => x.ScheduleMinuteColombia)
                .HasColumnName("ScheduleMinuteColombia")
                .IsRequired()
                .HasDefaultValue(30);

            builder.Property(x => x.WeekdaysOnly)
                .HasColumnName("WeekdaysOnly")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.EvaluatorWarningBusinessDays)
                .HasColumnName("EvaluatorWarningBusinessDays")
                .IsRequired()
                .HasDefaultValue(20);

            builder.Property(x => x.EvaluatorOverdueBusinessDays)
                .HasColumnName("EvaluatorOverdueBusinessDays")
                .IsRequired()
                .HasDefaultValue(25);

            builder.Property(x => x.CommitteeOverdueBusinessDays)
                .HasColumnName("CommitteeOverdueBusinessDays")
                .IsRequired()
                .HasDefaultValue(10);

            builder.Property(x => x.StudentObservationsBusinessDays)
                .HasColumnName("StudentObservationsBusinessDays")
                .IsRequired()
                .HasDefaultValue(15);

            builder.Property(x => x.DefenseReminderHours)
                .HasColumnName("DefenseReminderHours")
                .IsRequired()
                .HasDefaultValue(48);

            builder.Property(x => x.WarnOnLastAllowedPeriod)
                .HasColumnName("WarnOnLastAllowedPeriod")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.LastRunAt)
                .HasColumnName("LastRunAt")
                .IsRequired(false);

            builder.Property(x => x.LastRunEvaluatedCount)
                .HasColumnName("LastRunEvaluatedCount")
                .IsRequired(false);

            builder.Property(x => x.LastRunTriggeredCount)
                .HasColumnName("LastRunTriggeredCount")
                .IsRequired(false);

            builder.Property(x => x.LastRunSkippedCount)
                .HasColumnName("LastRunSkippedCount")
                .IsRequired(false);

            builder.Property(x => x.LastRunFailedCount)
                .HasColumnName("LastRunFailedCount")
                .IsRequired(false);
        }
    }
}
