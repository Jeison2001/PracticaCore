using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework para EmailNotificationConfig
    /// </summary>
    public class EmailNotificationConfigConfiguration : IEntityTypeConfiguration<EmailNotificationConfig>
    {
        public void Configure(EntityTypeBuilder<EmailNotificationConfig> builder)
        {
            // Tabla
            builder.ToTable("EmailNotificationConfig");
            
            // Clave primaria
            builder.HasKey(e => e.Id);
            
            // Propiedades
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
                
            builder.Property(e => e.EventName)
                .HasColumnName("eventname")
                .HasMaxLength(100)
                .IsRequired();
                
            builder.Property(e => e.SubjectTemplate)
                .HasColumnName("subjecttemplate")
                .IsRequired();
                
            builder.Property(e => e.BodyTemplate)
                .HasColumnName("bodytemplate")
                .IsRequired();
                
            builder.Property(e => e.IsActive)
                .HasColumnName("isactive")
                .IsRequired()
                .HasDefaultValue(true);

            // Configuración base heredada
            builder.Property(e => e.IdUserCreatedAt)
                .HasColumnName("idusercreatedat")
                .IsRequired();
                
            builder.Property(e => e.CreatedAt)
                .HasColumnName("createdat")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(e => e.IdUserUpdatedAt)
                .HasColumnName("iduserupdatedat");
                
            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updatedat");
                
            builder.Property(e => e.OperationRegister)
                .HasColumnName("operationregister")
                .HasMaxLength(50)
                .HasDefaultValue("INSERT");
                
            builder.Property(e => e.StatusRegister)
                .HasColumnName("statusregister")
                .IsRequired()
                .HasDefaultValue(true);

            // Índices
            builder.HasIndex(e => e.EventName)
                .IsUnique()
                .HasDatabaseName("IX_EmailNotificationConfig_EventName");
                
            builder.HasIndex(e => new { e.IsActive, e.StatusRegister })
                .HasDatabaseName("IX_EmailNotificationConfig_Active_Status");

            // Relaciones
            builder.HasMany(e => e.RecipientRules)
                .WithOne(r => r.EmailNotificationConfig)
                .HasForeignKey(r => r.EmailNotificationConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
