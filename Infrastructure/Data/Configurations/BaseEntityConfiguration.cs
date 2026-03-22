using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class BaseEntityConfiguration<T, TId> : IEntityTypeConfiguration<T>
        where T : BaseEntity<TId>
        where TId : struct
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("Id");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("CreatedAt");
            builder.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt"); ;
            builder.Property(e => e.IdUserUpdatedAt).HasColumnName("IdUserUpdatedAt");
            builder.Property(e => e.OperationRegister).IsRequired().HasMaxLength(250).HasColumnName("OperationRegister");
            builder.Property(e => e.StatusRegister).IsRequired().HasColumnName("StatusRegister");
        }
    }
}

