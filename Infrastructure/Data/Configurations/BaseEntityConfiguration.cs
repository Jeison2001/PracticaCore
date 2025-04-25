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

    public class FacultyConfiguration : BaseEntityConfiguration<Faculty, int>
    {
        public override void Configure(EntityTypeBuilder<Faculty> builder)
        {
            base.Configure(builder);
            builder.ToTable("faculty");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }

    public class AcademicProgramConfiguration : BaseEntityConfiguration<AcademicProgram, int>
    {
        public override void Configure(EntityTypeBuilder<AcademicProgram> builder)
        {
            base.Configure(builder);
            builder.ToTable("academicprogram");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
            // Configuración explícita de la relación con Faculty
            builder.HasOne(p => p.Faculty)
                    .WithMany(f => f.AcademicPrograms)
                    .HasForeignKey(p => p.IdFaculty);

            // Especificar el nombre exacto de la columna FK en la base de datos
            builder.Property(p => p.IdFaculty).HasColumnName("idfaculty");
        }
    }

    public class IdentificationTypeConfiguration : BaseEntityConfiguration<IdentificationType, int>
    {
        public override void Configure(EntityTypeBuilder<IdentificationType> builder)
        {
            base.Configure(builder);
            builder.ToTable("identificationtype");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }

    public class RoleConfiguration : BaseEntityConfiguration<Role, int>
    {
        public override void Configure(EntityTypeBuilder<Role> builder)
        {
            base.Configure(builder);
            builder.ToTable("role");
            builder.Property(e => e.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);
        }
    }

    public class UserConfiguration : BaseEntityConfiguration<User, int>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
            builder.ToTable("User");
            builder.Property(e => e.IdIdentificationType).HasColumnName("ididentificationtype");
            builder.Property(e => e.Identification).IsRequired().HasMaxLength(50).HasColumnName("identification");
            builder.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnName("email");
            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("firstname");
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("lastname");
            builder.Property(e => e.IdAcademicProgram).HasColumnName("idacademicprogram");
            builder.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("phonenumber").IsRequired(false);
            builder.Property(e => e.CurrentAcademicPeriod).HasMaxLength(20).HasColumnName("currentacademicperiod").IsRequired(false);
            builder.Property(e => e.CumulativeAverage).HasColumnName("cumulativeaverage").IsRequired(false);
            builder.Property(e => e.ApprovedCredits).HasColumnName("approvedcredits").IsRequired(false);
            builder.Property(e => e.TotalAcademicCredits).HasColumnName("totalacademiccredits").IsRequired(false);
            builder.Property(e => e.Observation).HasColumnName("observation").IsRequired(false);
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración explícita de la relación con IdentificationType
            builder.HasOne(u => u.IdentificationType)
                   .WithMany()
                   .HasForeignKey(u => u.IdIdentificationType);

            // Configuración explícita de la relación con AcademicProgram
            builder.HasOne(u => u.AcademicProgram)
                   .WithMany()
                   .HasForeignKey(u => u.IdAcademicProgram);
        }
    }

    public class UserRoleConfiguration : BaseEntityConfiguration<UserRole, int>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            base.Configure(builder);
            builder.ToTable("UserRole");
            builder.Property(e => e.IdUser).HasColumnName("iduser");
            builder.Property(e => e.IdRole).HasColumnName("idrole");
            builder.Property(e => e.IdUserCreatedAt).HasColumnName("idusercreatedat").IsRequired(false);

            // Configuración explícita de la relación con User
            builder.HasOne(ur => ur.User)
                   .WithMany(ur => ur.UserRoles)
                   .HasForeignKey(ur => ur.IdUser);

            // Configuración explícita de la relación con Role
            builder.HasOne(ur => ur.Role)
                   .WithMany(ur => ur.UserRole)
                   .HasForeignKey(ur => ur.IdRole);
        }
    }
}

