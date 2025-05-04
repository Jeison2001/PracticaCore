using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class StatusWorkGradeConfiguration : BaseEntityConfiguration<StatusWorkGrade, int>
    {
        public override void Configure(EntityTypeBuilder<StatusWorkGrade> builder)
        {
            base.Configure(builder);
            builder.ToTable("StatusWorkGrade");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description");
            builder.Property(e => e.IsFinalState).IsRequired().HasDefaultValue(false).HasColumnName("isfinalstate");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}
