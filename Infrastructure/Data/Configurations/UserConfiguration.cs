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
            builder.Property(e => e.IdIdentificationType).HasColumnName("ididentificationtype");
            builder.Property(e => e.Identification).IsRequired().HasMaxLength(50).HasColumnName("identification");
            builder.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnName("email");
            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("firstname");
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("lastname");
            builder.Property(e => e.IdAcademicProgram).HasColumnName("idacademicprogram");
            builder.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("phonenumber").IsRequired(false);
            builder.Property(e => e.CurrentAcademicPeriod).HasMaxLength(20).HasColumnName("currentacademicperiod").IsRequired(false);
            builder.Property(e => e.CumulativeAverage).HasColumnName("cumulativeaverage").IsRequired(false);
            builder.Property(e => e.ApprovedCredits).HasColumnName("approvedcredits").IsRequired(false);
            builder.Property(e => e.TotalAcademicCredits).HasColumnName("totalacademiccredits").IsRequired(false);
            builder.Property(e => e.Observation).HasColumnName("observation").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

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
