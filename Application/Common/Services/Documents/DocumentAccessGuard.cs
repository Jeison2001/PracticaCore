using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Documents;

namespace Application.Common.Services.Documents
{
    /// <summary>
    /// Guard de acceso a documentos. Centraliza la autorizacion a nivel de dato: es
    /// fail-closed, deniega salvo que el usuario sea administrador de configuracion documental,
    /// este explicitamente vinculado a la inscripcion (estudiante o docente confirmado) o
    /// posea un permiso transversal contextualmente valido para la modalidad y fase del documento.
    /// </summary>
    public class DocumentAccessGuard : IDocumentAccessGuard
    {
        // Permisos globales de administracion del catalogo/seguridad que habilitan lectura transversal
        // con independencia de la modalidad o fase de la inscripcion.
        private static readonly HashSet<string> GlobalDocumentAdminPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.Configuraciones.N3CFSU,
            PermissionCodes.Configuraciones.N2CFDO,
            PermissionCodes.Configuraciones.N3CFDOT,
        };

        private static readonly HashSet<string> EmptyPermissions = new(StringComparer.Ordinal);

        private static readonly HashSet<string> PgPropuestaPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPA,
        };

        private static readonly HashSet<string> PgAnteproyectoPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
        };

        private static readonly HashSet<string> PgProyectoPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.ProyectoGrado.N4PGPPRG,
        };

        private static readonly HashSet<string> PgSustentacionPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.ProyectoGrado.N4PGPSG,
        };

        private static readonly HashSet<string> ProyectoGradoAnyStagePermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
            PermissionCodes.ProyectoGrado.N4PGPPRG,
            PermissionCodes.ProyectoGrado.N4PGPSG,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> PaInscripcionPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
        };

        private static readonly HashSet<string> PaDesarrolloPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.PracticaAcademica.N4PAF1PG,
        };

        private static readonly HashSet<string> PaEvaluacionPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.PracticaAcademica.N4PAF2PG,
        };

        private static readonly HashSet<string> PracticaAcademicaAnyStagePermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> CoTerminalPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.CoTerminal.N3CTSGG,
            PermissionCodes.CoTerminal.N3CTSGR,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> SeminarioPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.Seminario.N3SASGG,
            PermissionCodes.Seminario.N3SASGR,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> PublicacionArticuloPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.PublicacionArticulo.N3PCSGG,
            PermissionCodes.PublicacionArticulo.N3PCSGR,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> GradoPromedioPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.GradoPromedio.N3GPSGG,
            PermissionCodes.GradoPromedio.N3GPSGR,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private static readonly HashSet<string> SaberProPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.SaberPro.N3SPSGG,
            PermissionCodes.SaberPro.N3SPSGR,
            PermissionCodes.ModalidadesGrado.N3MGPR,
        };

        private readonly IUnitOfWork _unitOfWork;

        public DocumentAccessGuard(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task EnsureUserCanModifyAsync(
            Document entity,
            int currentUserId,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<string>? userPermissions = null)
        {
            // Fail-closed: sin identidad de usuario => denegar.
            if (currentUserId <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para modificar este documento.");

            // Un documento sin inscripcion asociada no puede verificar membresia => denegar.
            if (entity.IdInscriptionModality is null or <= 0)
                throw new ForbiddenAccessException("El documento no esta asociado a ninguna inscripcion. No tienes permiso para modificarlo.");

            var inscriptionId = entity.IdInscriptionModality.Value;

            // Permitir si el usuario es estudiante vinculado a la inscripcion (registro activo).
            if (await IsStudentInInscriptionAsync(inscriptionId, currentUserId, cancellationToken))
                return;

            // O si es docente asignado activo, no revocado y confirmado en la inscripcion.
            var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            var isTeacherInInscription = await taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == currentUserId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);

            if (isTeacherInInscription)
                return;

            if (userPermissions != null &&
                userPermissions.Count > 0 &&
                await HasContextualReadPermissionAsync(
                    inscriptionId,
                    userPermissions,
                    stageModalityId: null,
                    documentTypeId: entity.IdDocumentType,
                    cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException("No tienes permiso para modificar este documento. No estas vinculado a esta inscripcion.");
        }

        public async Task EnsureUserCanReadAsync(
            int? inscriptionModalityId,
            CurrentUserInfo currentUser,
            int? stageModalityId = null,
            int? documentTypeId = null,
            CancellationToken cancellationToken = default)
        {
            // Fail-closed: sin identidad de usuario => denegar.
            if (currentUser?.UserId is null or <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para consultar este documento.");

            // Sin inscripcion asociada (nula o invalida) => denegar.
            if (inscriptionModalityId is null or <= 0)
                throw new ForbiddenAccessException("El documento no esta asociado a ninguna inscripcion. No tienes permiso para consultarlo.");

            if (currentUser.Permissions != null && currentUser.Permissions.Any(GlobalDocumentAdminPermissions.Contains))
                return;

            var userId = currentUser.UserId.Value;
            var inscriptionId = inscriptionModalityId.Value;

            if (await IsStudentInInscriptionAsync(inscriptionId, userId, cancellationToken))
                return;

            var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            var isConfirmedTeacher = await taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);

            if (isConfirmedTeacher)
                return;

            if (currentUser.Permissions != null &&
                currentUser.Permissions.Count > 0 &&
                await HasContextualReadPermissionAsync(
                    inscriptionId,
                    currentUser.Permissions,
                    stageModalityId,
                    documentTypeId,
                    cancellationToken))
            {
                return;
            }

            throw new ForbiddenAccessException("No tienes permiso para consultar este documento. No estas vinculado ni autorizado para esta modalidad o fase.");
        }

        private async Task<bool> HasContextualReadPermissionAsync(
            int inscriptionId,
            IReadOnlyCollection<string> userPermissions,
            int? stageModalityId,
            int? documentTypeId,
            CancellationToken cancellationToken)
        {
            var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscriptionId && x.StatusRegister, cancellationToken);

            if (inscription == null)
                return false;

            var modality = await _unitOfWork.GetRepository<Modality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscription.IdModality && x.StatusRegister, cancellationToken);

            if (modality == null || string.IsNullOrWhiteSpace(modality.Code))
                return false;

            var stageRepo = _unitOfWork.GetRepository<StageModality, int>();
            int? effectiveStageId = null;

            if (stageModalityId.HasValue && stageModalityId.Value > 0)
            {
                effectiveStageId = stageModalityId.Value;
            }
            else
            {
                if (documentTypeId.HasValue && documentTypeId.Value > 0)
                {
                    var docType = await _unitOfWork.GetRepository<DocumentType, int>()
                        .GetFirstOrDefaultAsync(x => x.Id == documentTypeId.Value && x.StatusRegister, cancellationToken);
                    effectiveStageId = docType?.IdStageModality;
                }

                effectiveStageId ??= inscription.IdStageModality;
            }

            StageModality? effectiveStage = null;
            if (effectiveStageId.HasValue)
            {
                effectiveStage = await stageRepo.GetFirstOrDefaultAsync(
                    x => x.Id == effectiveStageId.Value && x.StatusRegister,
                    cancellationToken);

                // Evita manipular idStageModality apuntando a otra modalidad distinta de la inscripcion.
                if (effectiveStage != null && effectiveStage.IdModality != modality.Id)
                    return false;
            }

            var allowedForTargetStage = GetContextualReadPermissions(modality.Code, effectiveStage?.Code);
            if (userPermissions.Any(allowedForTargetStage.Contains))
                return true;

            // Si se consulta un documento o filtro de una fase anterior a la fase actual de la inscripcion
            // dentro de la misma modalidad (ej. comite de Sustentacion consultando informe de Proyecto),
            // permitir tambien los permisos de gestion de la fase actual de la inscripcion.
            if (inscription.IdStageModality.HasValue &&
                effectiveStage != null &&
                inscription.IdStageModality.Value != effectiveStage.Id)
            {
                var inscriptionStage = await stageRepo.GetFirstOrDefaultAsync(
                    x => x.Id == inscription.IdStageModality.Value && x.StatusRegister,
                    cancellationToken);

                if (inscriptionStage != null &&
                    inscriptionStage.IdModality == modality.Id &&
                    effectiveStage.StageOrder <= inscriptionStage.StageOrder)
                {
                    var allowedForCurrentStage = GetContextualReadPermissions(modality.Code, inscriptionStage.Code);
                    if (userPermissions.Any(allowedForCurrentStage.Contains))
                        return true;
                }
            }

            return false;
        }

        private static IReadOnlySet<string> GetContextualReadPermissions(string modalityCode, string? stageCode)
        {
            return modalityCode.ToUpperInvariant() switch
            {
                ModalityCodes.ProyectoGrado => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PgFasePropuesta => PgPropuestaPermissions,
                    StageModalityCodes.PgFaseAnteproyecto => PgAnteproyectoPermissions,
                    StageModalityCodes.PgFaseProyectoInforme or StageModalityCodes.PgFaseProyecto => PgProyectoPermissions,
                    StageModalityCodes.PgFaseSustentacion => PgSustentacionPermissions,
                    null or "" => ProyectoGradoAnyStagePermissions,
                    _ => EmptyPermissions,
                },

                ModalityCodes.PracticaAcademica => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PaFaseInscripcion => PaInscripcionPermissions,
                    StageModalityCodes.PaFaseDesarrollo => PaDesarrolloPermissions,
                    StageModalityCodes.PaFaseEvaluacion => PaEvaluacionPermissions,
                    null or "" => PracticaAcademicaAnyStagePermissions,
                    _ => EmptyPermissions,
                },

                ModalityCodes.CoTerminal => CoTerminalPermissions,
                ModalityCodes.SeminarioAct => SeminarioPermissions,
                ModalityCodes.PublicacionArticulo => PublicacionArticuloPermissions,
                ModalityCodes.GradoPromedio => GradoPromedioPermissions,
                ModalityCodes.SaberPro => SaberProPermissions,
                _ => EmptyPermissions,
            };
        }

        private Task<bool> IsStudentInInscriptionAsync(int inscriptionId, int userId, CancellationToken cancellationToken)
        {
            var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            return uimRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId && x.IdUser == userId && x.StatusRegister,
                cancellationToken);
        }
    }
}
