using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class StateProjectFinalConfiguration : BaseEntityConfiguration<StateProjectFinal, int>
    {
        public override void Configure(EntityTypeBuilder<StateProjectFinal> builder)
        {
            base.Configure(builder);
            builder.ToTable("StateProjectFinal");
            builder.Property(e => e.Code).HasColumnName("code").IsRequired().HasMaxLength(100);
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
            builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(e => e.IsFinalState).HasColumnName("isfinalstate").IsRequired();
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat");
        }
    }
}
