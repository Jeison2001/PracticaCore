using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SeminarConfiguration : BaseEntityConfiguration<Seminar, int>
    {
        public override void Configure(EntityTypeBuilder<Seminar> builder)
        {
            base.Configure(builder);

            builder.ToTable("Seminar");

            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage").IsRequired();
            builder.Property(e => e.SeminarName).HasColumnName("seminarname").HasMaxLength(255).IsRequired(false);
            builder.Property(e => e.AttendancePercentage).HasColumnName("attendancepercentage").HasColumnType("numeric(5,2)").IsRequired(false);
            builder.Property(e => e.FinalGrade).HasColumnName("finalgrade").HasColumnType("numeric(3,2)").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("observations").HasColumnType("text").IsRequired(false);

            // Relationships
            builder.HasOne(e => e.InscriptionModality)
                .WithOne(im => im.Seminar)
                .HasForeignKey<Seminar>(e => e.Id);

            builder.HasOne(e => e.StateStage)
                .WithMany()
                .HasForeignKey(e => e.IdStateStage);
        }
    }
}
