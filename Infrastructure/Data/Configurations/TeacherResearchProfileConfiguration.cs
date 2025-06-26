using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TeacherResearchProfileConfiguration : BaseEntityConfiguration<TeacherResearchProfile, int>
    {
        public override void Configure(EntityTypeBuilder<TeacherResearchProfile> builder)
        {
            base.Configure(builder);
            builder.ToTable("TeacherResearchProfile");
            builder.Property(x => x.IdUser).HasColumnName("iduser").IsRequired();
            builder.Property(x => x.IdResearchLine).HasColumnName("idresearchline").IsRequired();
            builder.Property(x => x.IdResearchSubLine).HasColumnName("idresearchsubline");
            builder.Property(x => x.ProfileDescription).HasColumnName("profiledescription").HasMaxLength(255);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.IdUser);
            builder.HasOne(x => x.ResearchLine).WithMany().HasForeignKey(x => x.IdResearchLine);
            builder.HasOne(x => x.ResearchSubLine).WithMany().HasForeignKey(x => x.IdResearchSubLine);
        }
    }
}
