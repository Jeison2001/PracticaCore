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

        private static readonly HashSet<string> StageManagementPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.Configuraciones.N3CFSU,
            PermissionCodes.Configuraciones.N2CFDO,
            PermissionCodes.Configuraciones.N3CFDOT,
            PermissionCodes.ModalidadesGrado.N3MGPR,
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
            PermissionCodes.ProyectoGrado.N4PGPPRG,
            PermissionCodes.ProyectoGrado.N4PGPSG,
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
            PermissionCodes.CoTerminal.N3CTSGG,
            PermissionCodes.Seminario.N3SASGG,
            PermissionCodes.PublicacionArticulo.N3PCSGG,
            PermissionCodes.GradoPromedio.N3GPSGG,
            PermissionCodes.SaberPro.N3SPSGG,
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

            var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscriptionId && x.StatusRegister, cancellationToken);
            if (inscription == null)
                throw new ForbiddenAccessException("No tienes permiso para modificar este documento. La inscripcion no existe o esta inactiva.");

            var docType = await _unitOfWork.GetRepository<DocumentType, int>()
                .GetFirstOrDefaultAsync(x => x.Id == entity.IdDocumentType && x.StatusRegister, cancellationToken);
            if (docType == null)
                throw new ForbiddenAccessException("El tipo de documento especificado no existe o esta inactivo.");

            StageModality? docTypeStage = null;
            if (docType.IdStageModality.HasValue && docType.IdStageModality.Value > 0)
            {
                docTypeStage = await _unitOfWork.GetRepository<StageModality, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == docType.IdStageModality.Value && x.StatusRegister, cancellationToken);
                if (docTypeStage == null || docTypeStage.IdModality != inscription.IdModality)
                    throw new ForbiddenAccessException("El tipo de documento no corresponde a la modalidad de esta inscripcion.");
            }

            var docClass = await _unitOfWork.GetRepository<DocumentClass, int>()
                .GetByIdAsync(docType.IdDocumentClass);
            var docClassCode = docClass?.Code?.Trim().ToUpperInvariant() ?? string.Empty;

            var isEvaluationConcept = string.Equals(docClassCode, DocumentClassCodes.DocEvaluacionConcepto, StringComparison.Ordinal);
            var isAcademicRecord = string.Equals(docClassCode, DocumentClassCodes.DocAcademRegistro, StringComparison.Ordinal);
            var isGuideOrFormat = string.Equals(docClassCode, DocumentClassCodes.DocGuiaFormato, StringComparison.Ordinal);
            var isStaffDocument = isEvaluationConcept || isAcademicRecord;

            var isStudent = await IsStudentInInscriptionAsync(inscriptionId, currentUserId, cancellationToken);
            var modality = await _unitOfWork.GetRepository<Modality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscription.IdModality && x.StatusRegister, cancellationToken);
            var hasStageManagementPerm = HasStageManagementPermission(modality?.Code, docTypeStage?.Code, userPermissions);

            if (isGuideOrFormat)
            {
                if (!hasStageManagementPerm)
                    throw new ForbiddenAccessException("Solo la administracion o gestion de la etapa puede modificar documentos guia o formatos institucionales.");
            }
            else if (!isStaffDocument)
            {
                // Entregables del estudiante: estrictamente solo los estudiantes autores activos de la inscripcion.
                if (!isStudent)
                    throw new ForbiddenAccessException("Solo los estudiantes autores registrados en esta inscripcion pueden cargar, modificar o eliminar documentos de entrega del estudiante.");
            }
            else
            {
                // Un estudiante inscrito nunca puede emitir, modificar ni eliminar documentos de evaluacion o actas/registros academicos.
                if (isStudent)
                    throw new ForbiddenAccessException("Los estudiantes no pueden cargar, modificar ni eliminar documentos de evaluacion o registro academico.");

                var isConfirmedTeacher = await IsConfirmedTeacherInInscriptionAsync(inscriptionId, currentUserId, cancellationToken);
                var isProposalEvaluator =
                    isEvaluationConcept &&
                    string.Equals(modality?.Code, ModalityCodes.ProyectoGrado, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(docTypeStage?.Code, StageModalityCodes.PgFasePropuesta, StringComparison.OrdinalIgnoreCase) &&
                    userPermissions != null &&
                    userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPR);

                var canConfirmedTeacherWrite =
                    isEvaluationConcept &&
                    isConfirmedTeacher &&
                    HasConfirmedTeacherWritePermission(modality?.Code, docTypeStage?.Code, userPermissions);

                // Evaluaciones: docente confirmado de la inscripcion (con permiso de revision/gestion de la fase), evaluador de propuesta o gestion de la etapa.
                // Registros academicos oficiales (DOC_ACADEM_REGISTRO): exclusivamente gestion de la etapa.
                if (!canConfirmedTeacherWrite && !isProposalEvaluator && !hasStageManagementPerm)
                    throw new ForbiddenAccessException("No tienes permiso para cargar o modificar documentos de evaluacion o registro academico en esta inscripcion.");

                // En ediciones/eliminaciones sobre un documento de evaluacion/registro existente,
                // restringir al autor original (si sigue autorizado) o a gestion/administracion de la etapa.
                if (entity.Id > 0 &&
                    entity.IdUserCreatedAt.HasValue &&
                    entity.IdUserCreatedAt.Value != currentUserId &&
                    !hasStageManagementPerm)
                {
                    throw new ForbiddenAccessException("Solo el autor que emitio este documento de evaluacion o la gestion de la etapa pueden modificarlo.");
                }
            }

            await EnsureTargetStageAllowsDocumentMutationAsync(
                inscription,
                modality,
                docTypeStage,
                isAcademicRecord,
                isExistingDocument: entity.Id > 0,
                hasStageManagementPerm,
                cancellationToken);
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

            if (await IsConfirmedTeacherInInscriptionAsync(inscriptionId, userId, cancellationToken))
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
            int? effectiveStageId = stageModalityId is > 0 ? stageModalityId.Value : null;

            if (documentTypeId.HasValue && documentTypeId.Value > 0)
            {
                var docType = await _unitOfWork.GetRepository<DocumentType, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == documentTypeId.Value && x.StatusRegister, cancellationToken);
                if (docType == null)
                    return false;

                // Evitar contrabando de filtro dual: si stageModalityId y docType.IdStageModality apuntan a etapas distintas, denegar.
                if (effectiveStageId.HasValue &&
                    docType.IdStageModality.HasValue &&
                    docType.IdStageModality.Value > 0 &&
                    docType.IdStageModality.Value != effectiveStageId.Value)
                {
                    return false;
                }

                effectiveStageId ??= docType.IdStageModality;
            }

            effectiveStageId ??= inscription.IdStageModality;

            StageModality? effectiveStage = null;
            if (effectiveStageId.HasValue)
            {
                effectiveStage = await stageRepo.GetFirstOrDefaultAsync(
                    x => x.Id == effectiveStageId.Value && x.StatusRegister,
                    cancellationToken);

                // Fail-closed: un IdStageModality inexistente o de otra modalidad no debe autorizar acceso.
                if (effectiveStage == null || effectiveStage.IdModality != modality.Id)
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

        private Task<bool> IsConfirmedTeacherInInscriptionAsync(int inscriptionId, int userId, CancellationToken cancellationToken)
        {
            var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            return taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);
        }

        private static bool HasConfirmedTeacherWritePermission(
            string? modalityCode,
            string? stageCode,
            IReadOnlyCollection<string>? userPermissions)
        {
            if (userPermissions == null || userPermissions.Count == 0)
                return false;

            if (HasStageManagementPermission(modalityCode, stageCode, userPermissions))
                return true;

            if (string.IsNullOrWhiteSpace(modalityCode))
                return false;

            var contextualPerms = GetContextualReadPermissions(modalityCode, stageCode);
            if (userPermissions.Any(contextualPerms.Contains))
                return true;

            return modalityCode.ToUpperInvariant() switch
            {
                ModalityCodes.ProyectoGrado => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PgFasePropuesta =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPC) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPR),
                    StageModalityCodes.PgFaseAnteproyecto =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAR),
                    StageModalityCodes.PgFaseProyectoInforme or StageModalityCodes.PgFaseProyecto =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPRR),
                    StageModalityCodes.PgFaseSustentacion =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPSR),
                    null or "" =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPC) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPR) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAR) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPRR) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPSR),
                    _ => false,
                },
                ModalityCodes.PracticaAcademica => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PaFaseInscripcion =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PR),
                    StageModalityCodes.PaFaseDesarrollo =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF1PR),
                    StageModalityCodes.PaFaseEvaluacion =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF2PR),
                    null or "" =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PR) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF1PR) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF2PR),
                    _ => false,
                },
                _ => false,
            };
        }

        private static bool HasStageManagementPermission(
            string? modalityCode,
            string? stageCode,
            IReadOnlyCollection<string>? userPermissions)
        {
            if (userPermissions == null || userPermissions.Count == 0)
                return false;

            if (userPermissions.Any(GlobalDocumentAdminPermissions.Contains))
                return true;

            if (string.IsNullOrWhiteSpace(modalityCode))
                return false;

            return modalityCode.ToUpperInvariant() switch
            {
                ModalityCodes.ProyectoGrado => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PgFasePropuesta =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPG) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPA),
                    StageModalityCodes.PgFaseAnteproyecto =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAG) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAA),
                    StageModalityCodes.PgFaseProyectoInforme or StageModalityCodes.PgFaseProyecto =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPRG),
                    StageModalityCodes.PgFaseSustentacion =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPSG),
                    _ =>
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPG) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPA) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAG) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPAA) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPPRG) ||
                        userPermissions.Contains(PermissionCodes.ProyectoGrado.N4PGPSG),
                },
                ModalityCodes.PracticaAcademica => stageCode?.ToUpperInvariant() switch
                {
                    StageModalityCodes.PaFaseInscripcion =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PG) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PA),
                    StageModalityCodes.PaFaseDesarrollo =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF1PG),
                    StageModalityCodes.PaFaseEvaluacion =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF2PG),
                    _ =>
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PG) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF0PA) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF1PG) ||
                        userPermissions.Contains(PermissionCodes.PracticaAcademica.N4PAF2PG),
                },
                ModalityCodes.CoTerminal => userPermissions.Contains(PermissionCodes.CoTerminal.N3CTSGG),
                ModalityCodes.SeminarioAct => userPermissions.Contains(PermissionCodes.Seminario.N3SASGG),
                ModalityCodes.PublicacionArticulo => userPermissions.Contains(PermissionCodes.PublicacionArticulo.N3PCSGG),
                ModalityCodes.GradoPromedio => userPermissions.Contains(PermissionCodes.GradoPromedio.N3GPSGG),
                ModalityCodes.SaberPro => userPermissions.Contains(PermissionCodes.SaberPro.N3SPSGG),
                _ => false,
            };
        }

        private async Task EnsureTargetStageAllowsDocumentMutationAsync(
            InscriptionModality inscription,
            Modality? modality,
            StageModality? docTypeStage,
            bool isAcademicRecord,
            bool isExistingDocument,
            bool hasStageManagementPerm,
            CancellationToken cancellationToken)
        {
            // No bloquear por StateInscription.IsFinalStateForStage porque APROBADO marca esa bandera tras aprobar la inscripcion inicial.
            if (inscription.IsFinalRejection)
                throw new InvalidOperationException("La inscripcion se encuentra en estado de rechazo definitivo; no se permiten modificaciones de documentos.");

            var stateInscription = await _unitOfWork.GetRepository<StateInscription, int>()
                .GetByIdAsync(inscription.IdStateInscription);
            if (stateInscription != null && stateInscription.IsRejectionState)
                throw new InvalidOperationException($"La inscripcion se encuentra en estado de rechazo ('{stateInscription.Name}'); no se permiten modificaciones de documentos.");

            StageModality? currentInscriptionStage = null;
            if (inscription.IdStageModality.HasValue && inscription.IdStageModality.Value > 0)
            {
                currentInscriptionStage = await _unitOfWork.GetRepository<StageModality, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.IdStageModality.Value && x.StatusRegister, cancellationToken);
            }

            var allowOfficialRecordUploadOnNonRejectedClosedStage =
                isAcademicRecord && !isExistingDocument && hasStageManagementPerm;

            if (modality == null || string.IsNullOrWhiteSpace(modality.Code))
                return;

            var targetStateStage = await ResolveTargetStateStageAsync(
                inscription,
                modality.Code,
                docTypeStage ?? currentInscriptionStage,
                cancellationToken);

            if (targetStateStage != null && targetStateStage.IsRejectionState)
            {
                throw new InvalidOperationException($"La etapa se encuentra en estado de rechazo ('{targetStateStage.Name}'); no se permiten cargas ni modificaciones de documentos.");
            }

            var isSupersededStage =
                docTypeStage != null &&
                currentInscriptionStage != null &&
                docTypeStage.Id != currentInscriptionStage.Id &&
                docTypeStage.StageOrder < currentInscriptionStage.StageOrder;

            if (isSupersededStage)
            {
                var currentStateStage = await ResolveTargetStateStageAsync(
                    inscription,
                    modality.Code,
                    currentInscriptionStage,
                    cancellationToken);

                if (currentStateStage != null && currentStateStage.IsRejectionState)
                {
                    throw new InvalidOperationException($"La modalidad se encuentra en estado de rechazo ('{currentStateStage.Name}'); no se permiten cargas ni modificaciones de documentos.");
                }

                if (!allowOfficialRecordUploadOnNonRejectedClosedStage)
                {
                    throw new InvalidOperationException("La fase del tipo de documento ya fue superada y cerrada; no se permiten cargas, modificaciones ni eliminaciones.");
                }

                return;
            }

            if (targetStateStage != null &&
                (targetStateStage.IsFinalStateForStage || targetStateStage.IsFinalStateForModalityOverall) &&
                !allowOfficialRecordUploadOnNonRejectedClosedStage)
            {
                throw new InvalidOperationException($"La etapa actual ya se encuentra cerrada ('{targetStateStage.Name}'); no se permiten cargas, modificaciones ni eliminaciones de documentos.");
            }
        }

        private async Task<StateStage?> ResolveTargetStateStageAsync(
            InscriptionModality inscription,
            string modalityCode,
            StageModality? targetStage,
            CancellationToken cancellationToken)
        {
            int? stateStageId = null;
            var normalizedModality = modalityCode.ToUpperInvariant();

            if (normalizedModality == ModalityCodes.ProyectoGrado)
            {
                switch (targetStage?.Code?.ToUpperInvariant())
                {
                    case StageModalityCodes.PgFasePropuesta:
                        stateStageId = (await _unitOfWork.GetRepository<Proposal, int>()
                            .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
                        break;
                    case StageModalityCodes.PgFaseAnteproyecto:
                        stateStageId = (await _unitOfWork.GetRepository<PreliminaryProject, int>()
                            .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
                        break;
                    case StageModalityCodes.PgFaseProyectoInforme:
                    case StageModalityCodes.PgFaseProyecto:
                        stateStageId = (await _unitOfWork.GetRepository<ProjectFinal, int>()
                            .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
                        break;
                    case StageModalityCodes.PgFaseSustentacion:
                        var latestPresentation = (await _unitOfWork.GetRepository<ProjectPresentation, int>()
                            .GetAllAsync(
                                filter: x => x.IdInscriptionModality == inscription.Id && x.StatusRegister,
                                orderBy: q => q.OrderByDescending(x => x.AttemptNumber).ThenByDescending(x => x.Id),
                                pageNumber: 1,
                                pageSize: 1))
                            .FirstOrDefault();
                        stateStageId = latestPresentation?.IdStateStage;
                        break;
                }
            }
            else if (normalizedModality == ModalityCodes.PracticaAcademica)
            {
                stateStageId = (await _unitOfWork.GetRepository<AcademicPractice, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }
            else if (normalizedModality == ModalityCodes.CoTerminal)
            {
                stateStageId = (await _unitOfWork.GetRepository<CoTerminal, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }
            else if (normalizedModality == ModalityCodes.SeminarioAct)
            {
                stateStageId = (await _unitOfWork.GetRepository<Seminar, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }
            else if (normalizedModality == ModalityCodes.PublicacionArticulo)
            {
                stateStageId = (await _unitOfWork.GetRepository<ScientificArticle, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }
            else if (normalizedModality == ModalityCodes.GradoPromedio)
            {
                stateStageId = (await _unitOfWork.GetRepository<AcademicAverage, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }
            else if (normalizedModality == ModalityCodes.SaberPro)
            {
                stateStageId = (await _unitOfWork.GetRepository<SaberPro, int>()
                    .GetFirstOrDefaultAsync(x => x.Id == inscription.Id && x.StatusRegister, cancellationToken))?.IdStateStage;
            }

            if (stateStageId is null or <= 0)
                return null;

            return await _unitOfWork.GetRepository<StateStage, int>().GetByIdAsync(stateStageId.Value);
        }
    }
}
