using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserConfiguration : BaseEntityConfiguration<User, int>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
            builder.ToTable("User");
            builder.Property(e => e.IdIdentificationType).HasColumnName("IdIdentificationType");
            builder.Property(e => e.Identification).IsRequired().HasMaxLength(50).HasColumnName("Identification");
            builder.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnName("Email");
            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("FirstName");
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("LastName");
            builder.Property(e => e.IdAcademicProgram).HasColumnName("IdAcademicProgram");
            builder.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("PhoneNumber").IsRequired(false);
            builder.Property(e => e.CurrentAcademicPeriod).HasMaxLength(20).HasColumnName("CurrentAcademicPeriod").IsRequired(false);
            builder.Property(e => e.CumulativeAverage).HasColumnName("CumulativeAverage").IsRequired(false);
            builder.Property(e => e.ApprovedCredits).HasColumnName("ApprovedCredits").IsRequired(false);
            builder.Property(e => e.TotalAcademicCredits).HasColumnName("TotalAcademicCredits").IsRequired(false);
            builder.Property(e => e.Observation).HasColumnName("Observation").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);

            // Configuración explícita de la relación con IdentificationType
            builder.HasOne(u => u.IdentificationType)
                   .WithMany()
                   .HasForeignKey(u => u.IdIdentificationType);

            // Configuración explícita de la relación con AcademicProgram
            builder.HasOne(u => u.AcademicProgram)
                   .WithMany()
                   .HasForeignKey(u => u.IdAcademicProgram);
        }
    }
}
