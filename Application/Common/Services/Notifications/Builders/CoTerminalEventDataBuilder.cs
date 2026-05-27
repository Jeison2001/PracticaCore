using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class CoTerminalEventDataBuilder : MinorModalityEventDataBuilderBase, ICoTerminalEventDataBuilder
    {
        public CoTerminalEventDataBuilder(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger<CoTerminalEventDataBuilder> logger)
            : base(unitOfWork, studentDataService, logger) { }

        public async Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType)
        {
            try
            {
                var inscription = await GetInscriptionAsync(entityId);
                if (inscription == null) return [];

                var entityRepo = _unitOfWork.GetRepository<CoTerminal, int>();
                var entity = await entityRepo.GetByIdAsync(entityId);
                if (entity == null) return [];

                var data = await BuildBaseDataAsync(inscription, entity.IdStateStage, eventType, "Co-Terminal");
                data["Observations"] = entity.Observations ?? "Sin observaciones registradas";
                data["PostgraduateProgram"] = entity.PostgraduateProgramName ?? "-";
                data["University"] = entity.UniversityName ?? "-";
                data["Average"] = entity.FirstSemesterAverage?.ToString("F2") ?? "-";
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building CoTerminal event data"); return []; }
        }
    }
}
