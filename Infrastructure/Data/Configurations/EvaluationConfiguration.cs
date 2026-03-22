using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class EvaluationConfiguration : BaseEntityConfiguration<Evaluation, int>
    {
        public override void Configure(EntityTypeBuilder<Evaluation> builder)
        {
            base.Configure(builder);
            builder.ToTable("Evaluation");
            builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100).HasColumnName("EntityType");
            builder.Property(e => e.EntityId).IsRequired().HasColumnName("EntityId");
            builder.Property(e => e.IdEvaluationType).IsRequired().HasColumnName("IdEvaluationType");
            builder.Property(e => e.IdEvaluator).IsRequired().HasColumnName("IdEvaluator");
            builder.Property(e => e.Result).HasMaxLength(100).HasColumnName("Result").IsRequired(false);
            builder.Property(e => e.Observations).HasColumnName("Observations").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("IdUserCreatedAt").IsRequired(false);
            
            // Configure foreign key relationships
            builder.HasOne(e => e.EvaluationType)
                .WithMany(et => et.Evaluations)
                .HasForeignKey(e => e.IdEvaluationType);
                
            builder.HasOne(e => e.Evaluator)
                .WithMany()
                .HasForeignKey(e => e.IdEvaluator);
        }
    }
}