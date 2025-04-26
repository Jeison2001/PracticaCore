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
            builder.Property(e => e.Code).IsRequired().HasMaxLength(150).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(150).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.MaximumTermPeriods).HasColumnName("maximumtermperiods").IsRequired(false);
            builder.Property(e => e.AllowsExtension).IsRequired().HasColumnName("allowsextension").HasDefaultValue(false);
            builder.Property(e => e.RequiresDirector).IsRequired().HasColumnName("requiresdirector").HasDefaultValue(true);
            builder.Property(e => e.MaxStudents).IsRequired().HasColumnName("maxstudents").HasDefaultValue(1);
            builder.Property(e => e.SpecificRequirements).HasColumnName("specificrequirements").IsRequired(false);
            builder.Property(e => e.RequiresResearchHotbed).IsRequired().HasColumnName("requiresresearchhotbed").HasDefaultValue(false);
            builder.Property(e => e.RequiresApproval).IsRequired().HasColumnName("requiresapproval").HasDefaultValue(true);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}