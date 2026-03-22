using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AcademicPeriodConfiguration : BaseEntityConfiguration<AcademicPeriod, int>
    {
        public override void Configure(EntityTypeBuilder<AcademicPeriod> builder)
        {
            base.Configure(builder);
            builder.ToTable("AcademicPeriod");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("Code");
            builder.Property(e => e.StartDate).IsRequired().HasColumnName("StartDate");
            builder.Property(e => e.EndDate).IsRequired().HasColumnName("EndDate");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}