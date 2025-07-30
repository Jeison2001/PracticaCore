using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework para EmailRecipientRule
    /// </summary>
    public class EmailRecipientRuleConfiguration : IEntityTypeConfiguration<EmailRecipientRule>
    {
        public void Configure(EntityTypeBuilder<EmailRecipientRule> builder)
        {
            // Tabla
            builder.ToTable("EmailRecipientRule");
            
            // Clave primaria
            builder.HasKey(e => e.Id);
            
            // Propiedades
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
                
            builder.Property(e => e.EmailNotificationConfigId)
                .HasColumnName("emailnotificationconfigid")
                .IsRequired();
                
            builder.Property(e => e.RecipientType)
                .HasColumnName("recipienttype")
                .HasMaxLength(10)
                .IsRequired();
                
            builder.Property(e => e.RuleType)
                .HasColumnName("ruletype")
                .HasMaxLength(20)
                .IsRequired();
                
            builder.Property(e => e.RuleValue)
                .HasColumnName("rulevalue")
                .HasMaxLength(255)
                .IsRequired();
                
            builder.Property(e => e.Conditions)
                .HasColumnName("conditions")
                .HasColumnType("jsonb");
                
            builder.Property(e => e.Priority)
                .HasColumnName("priority")
                .IsRequired()
                .HasDefaultValue(1);

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
            builder.HasIndex(e => e.EmailNotificationConfigId)
                .HasDatabaseName("IX_EmailRecipientRule_ConfigId");
                
            builder.HasIndex(e => new { e.RuleType, e.Priority })
                .HasDatabaseName("IX_EmailRecipientRule_Type_Priority");

            // Relaciones
            builder.HasOne(e => e.EmailNotificationConfig)
                .WithMany(c => c.RecipientRules)
                .HasForeignKey(e => e.EmailNotificationConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            // Validaciones a nivel de base de datos
            builder.HasCheckConstraint("CK_EmailRecipientRule_RecipientType", 
                "recipienttype IN ('TO', 'CC', 'BCC')");
                
            builder.HasCheckConstraint("CK_EmailRecipientRule_RuleType", 
                "ruletype IN ('BY_ROLE', 'BY_ENTITY_RELATION', 'FIXED_EMAIL', 'EVENT_PARTICIPANT')");
        }
    }
}
