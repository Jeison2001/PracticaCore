using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ProposalConfiguration : BaseEntityConfiguration<Proposal, int>
    {
        public override void Configure(EntityTypeBuilder<Proposal> builder)
        {
            base.Configure(builder);
            builder.ToTable("Proposal");
            builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.GeneralObjective).HasColumnName("generalobjective").IsRequired();
            builder.Property(e => e.SpecificObjectives).HasColumnName("specificobjectives").HasColumnType("text[]").IsRequired();
            builder.Property(e => e.Observation).HasColumnName("observation").IsRequired(false);
            builder.Property(e => e.IdResearchLine).HasColumnName("idresearchline");
            builder.Property(e => e.IdResearchSubLine).HasColumnName("idresearchsubline");
            builder.Property(e => e.IdStateStage).HasColumnName("idstatestage");

            // Configure relationships
            builder.HasOne(p => p.InscriptionModality)
                .WithOne(i => i.Proposal)
                .HasForeignKey<Proposal>(p => p.Id);

            builder.HasOne(p => p.ResearchLine)
                .WithMany()
                .HasForeignKey(p => p.IdResearchLine);

            builder.HasOne(p => p.ResearchSubLine)
                .WithMany()
                .HasForeignKey(p => p.IdResearchSubLine);

            builder.HasOne(p => p.StateStage)
                .WithMany()
                .HasForeignKey(p => p.IdStateStage);
        }
    }
}