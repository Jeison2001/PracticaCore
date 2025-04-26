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
            builder.Property(e => e.IdResearchLine).HasColumnName("idresearchline");
            builder.Property(e => e.IdResearchSubLine).HasColumnName("idresearchsubline");
            builder.Property(e => e.IdStateProposal).HasColumnName("idstateproposal");

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

            builder.HasOne(p => p.StateProposal)
                .WithMany(s => s.Proposals)
                .HasForeignKey(p => p.IdStateProposal);
        }
    }
}