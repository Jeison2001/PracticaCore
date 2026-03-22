using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ModalityConfiguration : BaseEntityConfiguration<Modality, int>
    {
        public override void Configure(EntityTypeBuilder<Modality> builder)
        {
            base.Configure(builder);
            builder.ToTable("Modality");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(150).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(150).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.MaximumTermPeriods).HasColumnName("MaximumTermPeriods").IsRequired(false);
            builder.Property(e => e.AllowsExtension).IsRequired().HasColumnName("AllowsExtension").HasDefaultValue(false);
            builder.Property(e => e.RequiresDirector).IsRequired().HasColumnName("RequiresDirector").HasDefaultValue(true);
            builder.Property(e => e.MaxStudents).IsRequired().HasColumnName("MaxStudents").HasDefaultValue(1);
            builder.Property(e => e.MaxSpecificObjectives).HasColumnName("MaxSpecificObjectives").IsRequired(false);
            builder.Property(e => e.SpecificRequirements).HasColumnName("SpecificRequirements").IsRequired(false);
            builder.Property(e => e.RequiresResearchHotbed).IsRequired().HasColumnName("RequiresResearchHotbed").HasDefaultValue(false);
            builder.Property(e => e.RequiresApproval).IsRequired().HasColumnName("RequiresApproval").HasDefaultValue(true);
                builder.Property(e => e.RequiresSimpleDocumentation).IsRequired().HasColumnName("RequiresSimpleDocumentation").HasDefaultValue(true);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}