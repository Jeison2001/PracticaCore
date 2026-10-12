using System.Linq.Expressions;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            ConfigureFilterBuilderProviders();
        }

        public static void ConfigureFilterBuilderProviders()
        {
            if (Environment.GetEnvironmentVariable("UseInMemoryDatabase") == "true")
                return;

            var efFunctionsProp = typeof(EF).GetProperty(nameof(EF.Functions))!;
            var functionsExpr = Expression.Property(null, efFunctionsProp);
            var unaccentMethod = ((Func<string, string>)EF.Functions.Unaccent).Method;
            var iLikeMethod = ((Func<string, string, bool>)EF.Functions.ILike).Method;

            FilterBuilder.StringLikeProvider = (property, value) =>
            {
                var rawValue = value is ConstantExpression c && c.Value != null ? c.Value.ToString()! : value.ToString() ?? "";
                var cleanValue = FilterBuilder.RemoveDiacritics(rawValue).Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = "%" + cleanValue + "%";
                var patternExpr = Expression.Constant(pattern, typeof(string));

                var unaccentExpr = Expression.Call(null, unaccentMethod, functionsExpr, property);
                return Expression.Call(null, iLikeMethod, functionsExpr, unaccentExpr, patternExpr);
            };

            FilterBuilder.StringStartsWithProvider = (property, value) =>
            {
                var rawValue = value is ConstantExpression c && c.Value != null ? c.Value.ToString()! : value.ToString() ?? "";
                var cleanValue = FilterBuilder.RemoveDiacritics(rawValue).Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = cleanValue + "%";
                var patternExpr = Expression.Constant(pattern, typeof(string));

                var unaccentExpr = Expression.Call(null, unaccentMethod, functionsExpr, property);
                return Expression.Call(null, iLikeMethod, functionsExpr, unaccentExpr, patternExpr);
            };

            FilterBuilder.StringEndsWithProvider = (property, value) =>
            {
                var rawValue = value is ConstantExpression c && c.Value != null ? c.Value.ToString()! : value.ToString() ?? "";
                var cleanValue = FilterBuilder.RemoveDiacritics(rawValue).Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = "%" + cleanValue;
                var patternExpr = Expression.Constant(pattern, typeof(string));

                var unaccentExpr = Expression.Call(null, unaccentMethod, functionsExpr, property);
                return Expression.Call(null, iLikeMethod, functionsExpr, unaccentExpr, patternExpr);
            };
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }        // DbSets se añaden dinámicamente mediante reflexión o configuración
         public DbSet<User> Users => Set<User>();
         public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
         public DbSet<PreliminaryProject> PreliminaryProjects => Set<PreliminaryProject>();
         public DbSet<ProjectFinal> ProjectFinals => Set<ProjectFinal>();
         public DbSet<AcademicPractice> AcademicPractices => Set<AcademicPractice>();
         public DbSet<CoTerminal> CoTerminals => Set<CoTerminal>();
         public DbSet<Seminar> Seminars => Set<Seminar>();
         public DbSet<ScientificArticle> ScientificArticles => Set<ScientificArticle>();
         public DbSet<AcademicAverage> AcademicAverages => Set<AcademicAverage>();
         public DbSet<SaberPro> SaberPros => Set<SaberPro>();
         public DbSet<StageModality> StageModalities => Set<StageModality>();
         public DbSet<StateStage> StateStages => Set<StateStage>();
         public DbSet<DocumentClass> DocumentClasses => Set<DocumentClass>();
         public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
         public DbSet<Document> Documents => Set<Document>();
         public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();


         // Email Notification entities
         public DbSet<EmailNotificationConfig> EmailNotificationConfigs => Set<EmailNotificationConfig>();
         public DbSet<EmailRecipientRule> EmailRecipientRules => Set<EmailRecipientRule>();

         // Teacher enabled cargos
         public DbSet<TeacherEnabledCargo> TeacherEnabledCargos => Set<TeacherEnabledCargo>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseEntityConfiguration<,>).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
