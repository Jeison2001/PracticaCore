using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class AcademicAverageEventDataBuilder : MinorModalityEventDataBuilderBase, IAcademicAverageEventDataBuilder
    {
        public AcademicAverageEventDataBuilder(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger<AcademicAverageEventDataBuilder> logger)
            : base(unitOfWork, studentDataService, logger) { }

        public async Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType)
        {
            try
            {
                var inscription = await GetInscriptionAsync(entityId);
                if (inscription == null) return [];

                var entityRepo = _unitOfWork.GetRepository<AcademicAverage, int>();
                var entity = await entityRepo.GetByIdAsync(entityId);
                if (entity == null) return [];

                var data = await BuildBaseDataAsync(inscription, entity.IdStateStage, eventType, "Grado por Promedio");
                data["Observations"] = entity.Observations ?? "Sin observaciones registradas";
                data["CertifiedAverage"] = entity.CertifiedAverage?.ToString("F2") ?? "-";
                data["HasFailedSubjects"] = entity.HasFailedSubjects.HasValue ? (entity.HasFailedSubjects.Value ? "Sí" : "No") : "-";
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building AcademicAverage event data"); return []; }
        }
    }
}
