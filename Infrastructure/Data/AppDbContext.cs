using Domain.Entities;
using Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets se añaden dinámicamente mediante reflexión o configuración
        // Ejemplo para una entidad "Product":
         public DbSet<Example> Examples => Set<Example>();
         public DbSet<User> Users => Set<User>();
         public DbSet<PreliminaryProject> PreliminaryProjects => Set<PreliminaryProject>();
         public DbSet<ProjectFinal> ProjectFinals => Set<ProjectFinal>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseEntityConfiguration<,>).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
