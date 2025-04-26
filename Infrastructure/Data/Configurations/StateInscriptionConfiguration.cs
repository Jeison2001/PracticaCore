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
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IsSelectable).IsRequired().HasColumnName("isselectable").HasDefaultValue(true);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}