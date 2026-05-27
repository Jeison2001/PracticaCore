using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class SeminarEventDataBuilder : MinorModalityEventDataBuilderBase, ISeminarEventDataBuilder
    {
        public SeminarEventDataBuilder(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger<SeminarEventDataBuilder> logger)
            : base(unitOfWork, studentDataService, logger) { }

        public async Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType)
        {
            try
            {
                var inscription = await GetInscriptionAsync(entityId);
                if (inscription == null) return [];

                var entityRepo = _unitOfWork.GetRepository<Seminar, int>();
                var entity = await entityRepo.GetByIdAsync(entityId);
                if (entity == null) return [];

                var data = await BuildBaseDataAsync(inscription, entity.IdStateStage, eventType, "Seminario de Actualización");
                data["Observations"] = entity.Observations ?? "Sin observaciones registradas";
                data["SeminarName"] = entity.SeminarName ?? "-";
                data["Attendance"] = entity.AttendancePercentage?.ToString("F1") + "%" ?? "-";
                data["FinalGrade"] = entity.FinalGrade?.ToString("F2") ?? "-";
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building Seminar event data"); return []; }
        }
    }
}
