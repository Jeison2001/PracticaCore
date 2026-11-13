using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;

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
            CancellationToken cancellationToken)
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
        }
    }
}
