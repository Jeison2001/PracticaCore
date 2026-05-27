using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class ScientificArticleEventDataBuilder : MinorModalityEventDataBuilderBase, IScientificArticleEventDataBuilder
    {
        public ScientificArticleEventDataBuilder(IUnitOfWork unitOfWork, IStudentDataService studentDataService, ILogger<ScientificArticleEventDataBuilder> logger)
            : base(unitOfWork, studentDataService, logger) { }

        public async Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType)
        {
            try
            {
                var inscription = await GetInscriptionAsync(entityId);
                if (inscription == null) return [];

                var entityRepo = _unitOfWork.GetRepository<ScientificArticle, int>();
                var entity = await entityRepo.GetByIdAsync(entityId);
                if (entity == null) return [];

                var phaseLabel = GetPhaseLabel(entity.IdStateStage);
                var data = await BuildBaseDataAsync(inscription, entity.IdStateStage, eventType, phaseLabel);
                data["Observations"] = entity.Observations ?? "Sin observaciones registradas";
                data["ArticleTitle"] = entity.ArticleTitle ?? "-";
                data["JournalName"] = entity.JournalName ?? "-";
                data["ISSN"] = entity.ISSN ?? "-";
                data["JournalCategory"] = entity.JournalCategory ?? "-";
                data["AcceptanceDate"] = entity.AcceptanceDate?.ToString("dd/MM/yyyy") ?? "-";
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building ScientificArticle event data"); return []; }
        }

        private static string GetPhaseLabel(int stateStageId)
        {
            return stateStageId switch
            {
                >= 28 and <= 32 => "Artículo - Fase 1: Inscripción",
                >= 33 and <= 37 => "Artículo - Fase 2: Publicación",
                _ => "Artículo Científico"
            };
        }
    }
}
