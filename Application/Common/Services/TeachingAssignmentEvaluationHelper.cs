using Application.Common.Services.Jobs;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;

namespace Application.Common.Services
{
    /// <summary>
    /// Utilidades compartidas para validar cuotas MaxAssignments activas/confirmadas
    /// y evaluar la transición automática de PreliminaryProject a AP_EN_EVALUACION
    /// cuando existen al menos 2 jurados evaluadores confirmados.
    /// </summary>
    internal static class TeachingAssignmentEvaluationHelper
    {
        public const string JuradoEvaluadorCode = "JURADO_EVALUADOR";
        public const int MinimumConfirmedJurados = 2;

        private static readonly HashSet<string> RestrictedPracticaAcademicaCargos = new(StringComparer.OrdinalIgnoreCase)
        {
            "DIRECTOR",
            "CO_DIRECTOR",
            "ASESOR"
        };

        public static async Task ValidateCargoAllowedForModalityAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            TypeTeachingAssignment? type,
            CancellationToken cancellationToken)
        {
            if (type == null || string.IsNullOrWhiteSpace(type.Code) || !RestrictedPracticaAcademicaCargos.Contains(type.Code.Trim()))
                return;

            var inscription = await unitOfWork.GetRepository<InscriptionModality, int>().GetByIdAsync(inscriptionModalityId);
            if (inscription == null)
                return;

            var modality = await unitOfWork.GetRepository<Modality, int>().GetByIdAsync(inscription.IdModality);
            if (modality != null &&
                string.Equals(modality.Code, ModalityCodes.PracticaAcademica, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "En Práctica Académica no se permite asignar cargos de Director, Co-Director ni Asesor; solo Jurado Evaluador.");
            }
        }

        public static async Task ValidateMaxAssignmentsAsync(
            IRepository<TeachingAssignment, int> repository,
            TypeTeachingAssignment type,
            int teacherId,
            int? excludeAssignmentId,
            CancellationToken cancellationToken)
        {
            if (!type.MaxAssignments.HasValue)
                return;

            var count = excludeAssignmentId.HasValue
                ? await repository.CountAsync(
                    x => x.IdTeacher == teacherId &&
                         x.IdTypeTeachingAssignment == type.Id &&
                         x.StatusRegister &&
                         x.RevocationDate == null &&
                         x.IsConfirmed &&
                         x.Id != excludeAssignmentId.Value,
                    cancellationToken)
                : await repository.CountAsync(
                    x => x.IdTeacher == teacherId &&
                         x.IdTypeTeachingAssignment == type.Id &&
                         x.StatusRegister &&
                         x.RevocationDate == null &&
                         x.IsConfirmed,
                    cancellationToken);

            if (count >= type.MaxAssignments.Value)
                throw new InvalidOperationException($"El docente ya tiene el máximo permitido ({type.MaxAssignments}) de asignaciones activas para este cargo.");
        }

        public static async Task<int> CountConfirmedJuradosAsync(
            IUnitOfWork unitOfWork,
            IRepository<TeachingAssignment, int> teachingAssignmentRepository,
            int inscriptionModalityId,
            int? knownJuradoTypeId,
            CancellationToken cancellationToken)
        {
            var typeRepo = unitOfWork.GetRepository<TypeTeachingAssignment, int>();
            var juradoTypes = await typeRepo.GetAllAsync(
                t => t.Code == JuradoEvaluadorCode && t.StatusRegister);
            var juradoTypeIds = juradoTypes.Select(t => t.Id).ToList();

            if (juradoTypeIds.Count == 0 && knownJuradoTypeId.HasValue)
                juradoTypeIds.Add(knownJuradoTypeId.Value);

            if (juradoTypeIds.Count == 0)
                return 0;

            return await teachingAssignmentRepository.CountAsync(
                ta => ta.IdInscriptionModality == inscriptionModalityId &&
                      juradoTypeIds.Contains(ta.IdTypeTeachingAssignment) &&
                      ta.StatusRegister &&
                      ta.RevocationDate == null &&
                      ta.IsConfirmed,
                cancellationToken);
        }

        public static async Task TryAdvancePreliminaryProjectToEvaluationAsync(
            IUnitOfWork unitOfWork,
            IRepository<TeachingAssignment, int> teachingAssignmentRepository,
            int inscriptionModalityId,
            int? knownJuradoTypeId,
            int? userId,
            CancellationToken cancellationToken,
            IJobEnqueuer? jobEnqueuer = null)
        {
            var preliminaryRepo = unitOfWork.GetRepository<PreliminaryProject, int>();
            var preliminary = await preliminaryRepo.GetByIdAsync(inscriptionModalityId);
            if (preliminary == null)
                return;

            var stateStageRepo = unitOfWork.GetRepository<StateStage, int>();
            var currentState = await stateStageRepo.GetByIdAsync(preliminary.IdStateStage);
            if (currentState == null || currentState.Code != StateStageCodes.ApRadicadoPendAsigEval)
                return;

            var confirmedJuradosCount = await CountConfirmedJuradosAsync(
                unitOfWork,
                teachingAssignmentRepository,
                inscriptionModalityId,
                knownJuradoTypeId,
                cancellationToken);

            if (confirmedJuradosCount < MinimumConfirmedJurados)
                return;

            var enEvaluacionState = await stateStageRepo.GetFirstOrDefaultAsync(
                s => s.Code == StateStageCodes.ApEnEvaluacion && s.StatusRegister,
                cancellationToken);
            if (enEvaluacionState == null)
                return;

            preliminary.IdUserUpdatedAt = userId;
            preliminary.UpdatedAt = DateTimeOffset.UtcNow;
            preliminary.IdStateStage = enEvaluacionState.Id;

            await preliminaryRepo.UpdatePartialAsync(preliminary, [
                x => x.IdStateStage,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt
            ]);
            await unitOfWork.CommitAsync(cancellationToken);

            jobEnqueuer?.Enqueue<INotificationBackgroundJob>(
                x => x.HandlePreliminaryProjectChangeAsync(preliminary.Id, currentState.Id));
        }
    }
}
