using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Startup
{
    public class EnumConsistencyWorker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EnumConsistencyWorker> _logger;

        public EnumConsistencyWorker(IServiceProvider serviceProvider, ILogger<EnumConsistencyWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database is created (optional, depending on strategy)
            // await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            _logger.LogInformation("Verifying consistency between StateStageCodeEnum and Database...");

            var dbCodes = await dbContext.StateStages
                .Select(s => s.Code)
                .ToListAsync(cancellationToken);

            var enumCodes = Enum.GetNames(typeof(StateStageCodeEnum));

            var missingInDb = enumCodes.Except(dbCodes).ToList();
            var missingInEnum = dbCodes.Except(enumCodes).ToList();

            if (missingInDb.Any())
            {
                var error = $"CRITICAL: The following Enum codes are missing in the Database (StateStage table): {string.Join(", ", missingInDb)}";
                _logger.LogCritical(error);
                // Uncomment to enforce fail-fast
                // throw new Exception(error); 
            }

            if (missingInEnum.Any())
            {
                _logger.LogWarning($"The following Database codes are not defined in StateStageCodeEnum: {string.Join(", ", missingInEnum)}");
            }

            if (!missingInDb.Any())
            {
                _logger.LogInformation("StateStageCodeEnum consistency check passed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
