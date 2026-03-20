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
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("createdat");
            builder.Property(e => e.UpdatedAt).HasColumnName("updatedat"); ;
            builder.Property(e => e.IdUserUpdatedAt).HasColumnName("iduserupdatedat");
            builder.Property(e => e.OperationRegister).IsRequired().HasMaxLength(250).HasColumnName("operationregister");
            builder.Property(e => e.StatusRegister).IsRequired().HasColumnName("statusregister");
        }
    }
}

