using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services
{
    /// <summary>
    /// Centraliza verificaciones de membresía estudiantil, asignación docente confirmada
    /// y protección anti-auto-aprobación para modalidades y endpoints transversales.
    /// </summary>
    public static class ModalityAccessAuthorizationHelper
    {
        public static async Task<bool> IsUserInInscriptionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int userId,
            CancellationToken cancellationToken = default,
            bool requireConfirmed = true)
        {
            var userInscriptionRepo = unitOfWork.GetRepository<UserInscriptionModality, int>();
            return await userInscriptionRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionModalityId &&
                     x.IdUser == userId &&
                     x.StatusRegister &&
                     (!requireConfirmed || x.IsConfirmed),
                cancellationToken);
        }

        public static async Task<bool> IsConfirmedTeacherInInscriptionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var taRepo = unitOfWork.GetRepository<TeachingAssignment, int>();
            return await taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionModalityId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);
        }

        public static async Task EnsureConfirmedTeacherInInscriptionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            CurrentUserInfo currentUser,
            string forbiddenMessage,
            CancellationToken cancellationToken = default)
        {
            if (currentUser.UserId is not > 0 ||
                !await IsConfirmedTeacherInInscriptionAsync(unitOfWork, inscriptionModalityId, currentUser.UserId.Value, cancellationToken))
            {
                throw new ForbiddenAccessException(forbiddenMessage);
            }
        }

        public static async Task EnsureStudentAuthorizedForInscriptionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int? newStateStageId,
            CurrentUserInfo currentUser,
            string notMemberMessage,
            string selfApprovalMessage,
            CancellationToken cancellationToken = default)
        {
            if (currentUser.UserId is not > 0 ||
                !await IsUserInInscriptionAsync(unitOfWork, inscriptionModalityId, currentUser.UserId.Value, cancellationToken))
            {
                throw new ForbiddenAccessException(notMemberMessage);
            }

            if (newStateStageId.HasValue)
            {
                var stateStageRepo = unitOfWork.GetRepository<StateStage, int>();
                var newState = await stateStageRepo.GetByIdAsync(newStateStageId.Value)
                    ?? throw new InvalidOperationException("El estado destino especificado no existe.");

                if (newState.IsFinalStateForStage || newState.IsFinalStateForModalityOverall || newState.IsRejectionState)
                {
                    throw new ForbiddenAccessException(selfApprovalMessage);
                }
            }
        }

        public static async Task EnsureEvaluatorOrStudentAuthorizedForPatchAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int? newStateStageId,
            CurrentUserInfo? currentUser,
            string[] managementPerms,
            string evaluatorPerm,
            string teacherForbiddenMessage,
            string studentNotMemberMessage,
            string studentSelfApprovalMessage,
            CancellationToken cancellationToken = default)
        {
            if (currentUser == null ||
                currentUser.UserId is not > 0 ||
                currentUser.Permissions is not { Count: > 0 } perms)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para realizar esta acción.");
            }

            if (managementPerms.Any(perms.Contains))
            {
                return;
            }

            if (perms.Contains(evaluatorPerm))
            {
                await EnsureConfirmedTeacherInInscriptionAsync(
                    unitOfWork,
                    inscriptionModalityId,
                    currentUser,
                    teacherForbiddenMessage,
                    cancellationToken);
            }
            else
            {
                await EnsureStudentAuthorizedForInscriptionAsync(
                    unitOfWork,
                    inscriptionModalityId,
                    newStateStageId,
                    currentUser,
                    studentNotMemberMessage,
                    studentSelfApprovalMessage,
                    cancellationToken);
            }
        }
    }
}
