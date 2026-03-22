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
            builder.ToTable("EmailRecipientRule", t => {
                t.HasCheckConstraint("CK_EmailRecipientRule_RecipientType", "recipienttype IN ('TO', 'CC', 'BCC')");
                t.HasCheckConstraint("CK_EmailRecipientRule_RuleType", "ruletype IN ('BY_ROLE', 'BY_ENTITY_RELATION', 'FIXED_EMAIL', 'EVENT_PARTICIPANT')");
            });
            
            // Clave primaria
            builder.HasKey(e => e.Id);
            
            // Propiedades
            builder.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();
                
            builder.Property(e => e.EmailNotificationConfigId)
                .HasColumnName("EmailNotificationConfigId")
                .IsRequired();
                
            builder.Property(e => e.RecipientType)
                .HasColumnName("RecipientType")
                .HasMaxLength(10)
                .IsRequired();
                
            builder.Property(e => e.RuleType)
                .HasColumnName("RuleType")
                .HasMaxLength(20)
                .IsRequired();
                
            builder.Property(e => e.RuleValue)
                .HasColumnName("RuleValue")
                .HasMaxLength(255)
                .IsRequired();
                
            builder.Property(e => e.Conditions)
                .HasColumnName("Conditions")
                .HasColumnType("jsonb");
                
            builder.Property(e => e.Priority)
                .HasColumnName("Priority")
                .IsRequired()
                .HasDefaultValue(1);

            // Configuración base heredada
            builder.Property(e => e.IdUserCreatedAt)
                .HasColumnName("IdUserCreatedAt")
                .IsRequired(false);
                
            builder.Property(e => e.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(e => e.IdUserUpdatedAt)
                .HasColumnName("IdUserUpdatedAt");
                
            builder.Property(e => e.UpdatedAt)
                .HasColumnName("UpdatedAt");
                
            builder.Property(e => e.OperationRegister)
                .HasColumnName("OperationRegister")
                .HasMaxLength(50)
                .HasDefaultValue("INSERT");
                
            builder.Property(e => e.StatusRegister)
                .HasColumnName("StatusRegister")
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
        }
    }
}
