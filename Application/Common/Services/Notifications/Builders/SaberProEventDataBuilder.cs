using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class SaberProEventDataBuilder : MinorModalityEventDataBuilderBase, ISaberProEventDataBuilder
    {
        public SaberProEventDataBuilder(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger<SaberProEventDataBuilder> logger)
            : base(unitOfWork, studentDataService, logger) { }

        public async Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType)
        {
            try
            {
                var inscription = await GetInscriptionAsync(entityId);
                if (inscription == null) return [];

                var entityRepo = _unitOfWork.GetRepository<SaberPro, int>();
                var entity = await entityRepo.GetByIdAsync(entityId);
                if (entity == null) return [];

                var data = await BuildBaseDataAsync(inscription, entity.IdStateStage, eventType, "Saber Pro");
                data["Observations"] = entity.Observations ?? "Sin observaciones registradas";
                data["ExamDate"] = entity.ExamDate?.ToString("dd/MM/yyyy") ?? "-";
                data["ResultQuintile"] = entity.ResultQuintile?.ToString() ?? "-";
                data["ResultScore"] = entity.ResultScore?.ToString() ?? "-";
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building SaberPro event data"); return []; }
        }
    }
}
