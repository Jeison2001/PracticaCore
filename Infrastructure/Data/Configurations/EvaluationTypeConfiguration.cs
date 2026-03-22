using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class EvaluationTypeConfiguration : BaseEntityConfiguration<EvaluationType, int>
    {
        public override void Configure(EntityTypeBuilder<EvaluationType> builder)
        {
            base.Configure(builder);
            builder.ToTable("EvaluationType");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}