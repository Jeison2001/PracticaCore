using Application.Common.Helpers;
using Domain.Entities;
using Domain.Enums;
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

                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(entity.IdStateStage);

                var effectiveStateStageId = entity.IdStateStage;
                var effectiveStateCode = stateStage?.Code ?? string.Empty;

                // Cuando AdvanceScientificArticlePhaseHandler auto-promueve a ART_PUB_PENDIENTE dentro del mismo
                // CommitAsync, la notificación ART_FASE1_APPROVED debe reflejar el estado aprobado de Fase 1.
                if (string.Equals(eventType, "ART_FASE1_APPROVED", StringComparison.OrdinalIgnoreCase) &&
                    effectiveStateCode.StartsWith("ART_PUB_", StringComparison.OrdinalIgnoreCase))
                {
                    var approvedPhase1State = await stateStageRepo.GetFirstOrDefaultAsync(
                        s => s.Code == nameof(StateStageCodeEnum.ART_INS_APROBADO) && s.StatusRegister,
                        CancellationToken.None);

                    if (approvedPhase1State != null)
                    {
                        effectiveStateStageId = approvedPhase1State.Id;
                        effectiveStateCode = approvedPhase1State.Code;
                    }
                    else
                    {
                        effectiveStateCode = nameof(StateStageCodeEnum.ART_INS_APROBADO);
                    }
                }

                var phaseLabel = GetPhaseLabel(effectiveStateCode);
                var data = await BuildBaseDataAsync(
                    inscription,
                    effectiveStateStageId,
                    eventType,
                    phaseLabel,
                    entity.UpdatedAt ?? entity.CreatedAt,
                    entity.Observations);
                data["ArticleTitle"] = entity.ArticleTitle ?? "-";
                data["JournalName"] = entity.JournalName ?? "-";
                data["ISSN"] = entity.ISSN ?? "-";
                data["JournalCategory"] = entity.JournalCategory ?? "-";
                data["AcceptanceDate"] = NotificationFormattingHelper.ToColombiaCalendarDate(entity.AcceptanceDate, "-");
                return data;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error building ScientificArticle event data"); return []; }
        }

        private static string GetPhaseLabel(string stateStageCode)
        {
            if (stateStageCode.StartsWith("ART_INS_", StringComparison.OrdinalIgnoreCase)) return "Artículo - Fase 1: Inscripción";
            if (stateStageCode.StartsWith("ART_PUB_", StringComparison.OrdinalIgnoreCase)) return "Artículo - Fase 2: Publicación";
            return "Artículo Científico";
        }
    }
}
