// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Infrastructure\Data\Configurations\StateInscriptionConfiguration.cs
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class StateInscriptionConfiguration : BaseEntityConfiguration<StateInscription, int>
    {
        public override void Configure(EntityTypeBuilder<StateInscription> builder)
        {
            base.Configure(builder);
            builder.ToTable("StateInscription");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasColumnName("Code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("Name");
            builder.Property(e => e.Description).HasColumnName("Description").IsRequired(false);
            builder.Property(e => e.IsInitialState).IsRequired().HasColumnName("IsInitialState").HasDefaultValue(false);
            builder.Property(e => e.IsFinalStateForStage).IsRequired().HasColumnName("IsFinalStateForStage").HasDefaultValue(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
        }
    }
}