using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class AcademicPeriodConfiguration : BaseEntityConfiguration<AcademicPeriod, long>
    {
        public override void Configure(EntityTypeBuilder<AcademicPeriod> builder)
        {
            base.Configure(builder);
            builder.ToTable("AcademicPeriod");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.StartDate).IsRequired().HasColumnName("startdate");
            builder.Property(e => e.EndDate).IsRequired().HasColumnName("enddate");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}