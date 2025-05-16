using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class StateWorkGradeConfiguration : BaseEntityConfiguration<StateWorkGrade, int>
    {
        public override void Configure(EntityTypeBuilder<StateWorkGrade> builder)
        {
            base.Configure(builder);
            builder.ToTable("StateWorkGrade");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(100).HasColumnName("code");
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnType("text").HasColumnName("description");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            // Relación con InscriptionModality
            builder.HasMany(e => e.InscriptionModalities)
                   .WithOne(im => im.StateWorkGrade)
                   .HasForeignKey(im => im.IdStateWorkGrade)
                   .IsRequired(false);
        }
    }
}
