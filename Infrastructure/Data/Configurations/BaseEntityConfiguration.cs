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
            builder.Property(e => e.OperationRegister).IsRequired().HasColumnName("operationregister");
            builder.Property(e => e.StatusRegister).IsRequired().HasColumnName("statusregister");
        }
    }
    public class CountryConfiguration : BaseEntityConfiguration<Example, int>
    {
        public override void Configure(EntityTypeBuilder<Example> builder)
        {
            base.Configure(builder);
            builder.ToTable("example");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }
}
