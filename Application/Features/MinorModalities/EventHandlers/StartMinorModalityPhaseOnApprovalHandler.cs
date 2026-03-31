using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.MinorModalities.EventHandlers
{
    public class StartMinorModalityPhaseOnApprovalHandler : INotificationHandler<InscriptionStateChangedEvent>
    {
        // Mapa de código de Modalidad → permisos iniciales a asignar.
        // Reflejo exacto de la lógica del trigger fn_minor_modalities_flow.
        private static readonly Dictionary<string, string[]> _permissionsByModality = new()
        {
            [ModalityCodes.CoTerminal]          = [PermissionCodes.CoTerminal.N1CT,  PermissionCodes.CoTerminal.N2CTC],
            [ModalityCodes.SeminarioAct]        = [PermissionCodes.Seminario.N1SA,   PermissionCodes.Seminario.N2SAC],
            [ModalityCodes.PublicacionArticulo] = [PermissionCodes.PublicacionArticulo.N1PC, PermissionCodes.PublicacionArticulo.N2PCC],
            [ModalityCodes.GradoPromedio]       = [PermissionCodes.GradoPromedio.N1GP, PermissionCodes.GradoPromedio.N2GPES, PermissionCodes.GradoPromedio.N2GPR],
            [ModalityCodes.SaberPro]            = [PermissionCodes.SaberPro.N1SP,   PermissionCodes.SaberPro.N2SPC],
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StartMinorModalityPhaseOnApprovalHandler> _logger;

        public StartMinorModalityPhaseOnApprovalHandler(IUnitOfWork unitOfWork, ILogger<StartMinorModalityPhaseOnApprovalHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(InscriptionStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

            var modality = await modalityRepo.GetByIdAsync(notification.ModalityId);
            if (modality == null || !_permissionsByModality.ContainsKey(modality.Code)) return;

            var stateInscription = await stateInscriptionRepo.GetByIdAsync(notification.NewStateInscriptionId);
            if (stateInscription == null) return;

            // Lógica idéntica al trigger original:
            // RequiresApproval=TRUE → activa en UPDATE a APROBADO
            // RequiresApproval=FALSE → activa en INSERT/evento con NO_APLICA (modalidad sin proceso de aprobación formal)
            var expectedCode = modality.RequiresApproval ? StateInscriptionCodes.Aprobado : StateInscriptionCodes.NoAplica;
            if (stateInscription.Code != expectedCode) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            // Buscar la Fase 1 específica a esta Modalidad (StageOrder=1)
            var targetStageModality = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.IdModality == modality.Id && x.StageOrder == 1 && x.StatusRegister, cancellationToken);

            if (targetStageModality == null)
            {
                _logger.LogWarning("{Handler}: No se encontró Fase 1 activa para modalidad {code}.", nameof(StartMinorModalityPhaseOnApprovalHandler), modality.Code);
                return;
            }

            // Buscar el estado inicial (PENDIENTE) de esa fase
            var targetStateStage = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.IdStageModality == targetStageModality.Id && x.IsInitialState && x.StatusRegister, cancellationToken);

            if (targetStateStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró estado inicial activo para Fase 1 de {code}.", nameof(StartMinorModalityPhaseOnApprovalHandler), modality.Code);
                return;
            }

            // Crear el registro de extensión correspondiente a la modalidad (idempotente)
            await CreateExtensionRecordIfNotExistsAsync(modality.Code, notification, targetStateStage.Id, cancellationToken);

            // Actualizar la inscripción con la fase recién iniciada
            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += $" | Inicio automático fase 1 ({modality.Code})";

            await inscriptionModalityRepo.UpdatePartialAsync(inscription, [
                x => x.IdStageModality,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt,
                x => x.OperationRegister
            ]);

            // Asignar permisos iniciales al estudiante
            await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                _unitOfWork,
                notification.InscriptionModalityId,
                _permissionsByModality[modality.Code],
                notification.TriggeredByUserId,
                cancellationToken);
        }

        private async Task CreateExtensionRecordIfNotExistsAsync(
            string modalityCode,
            InscriptionStateChangedEvent notification,
            int targetStateStageId,
            CancellationToken cancellationToken)
        {
            const string operationRegister = "Creado automáticamente por DomainEvent";
            var id = notification.InscriptionModalityId;
            var userId = notification.TriggeredByUserId;

            switch (modalityCode)
            {
                case ModalityCodes.CoTerminal:
                    var coTerminalRepo = _unitOfWork.GetRepository<CoTerminal, int>();
                    if (!await coTerminalRepo.AnyAsync(x => x.Id == id, cancellationToken))
                        await coTerminalRepo.AddAsync(new CoTerminal { Id = id, IdStateStage = targetStateStageId, IdUserCreatedAt = userId, OperationRegister = operationRegister, StatusRegister = true, CreatedAt = DateTimeOffset.UtcNow });
                    break;

                case ModalityCodes.SeminarioAct:
                    var seminarRepo = _unitOfWork.GetRepository<Seminar, int>();
                    if (!await seminarRepo.AnyAsync(x => x.Id == id, cancellationToken))
                        await seminarRepo.AddAsync(new Seminar { Id = id, IdStateStage = targetStateStageId, IdUserCreatedAt = userId, OperationRegister = operationRegister, StatusRegister = true, CreatedAt = DateTimeOffset.UtcNow });
                    break;

                case ModalityCodes.PublicacionArticulo:
                    var articleRepo = _unitOfWork.GetRepository<ScientificArticle, int>();
                    if (!await articleRepo.AnyAsync(x => x.Id == id, cancellationToken))
                        await articleRepo.AddAsync(new ScientificArticle { Id = id, IdStateStage = targetStateStageId, IdUserCreatedAt = userId, OperationRegister = operationRegister, StatusRegister = true, CreatedAt = DateTimeOffset.UtcNow });
                    break;

                case ModalityCodes.GradoPromedio:
                    var avgRepo = _unitOfWork.GetRepository<AcademicAverage, int>();
                    if (!await avgRepo.AnyAsync(x => x.Id == id, cancellationToken))
                        await avgRepo.AddAsync(new AcademicAverage { Id = id, IdStateStage = targetStateStageId, IdUserCreatedAt = userId, OperationRegister = operationRegister, StatusRegister = true, CreatedAt = DateTimeOffset.UtcNow });
                    break;

                case ModalityCodes.SaberPro:
                    var saberProRepo = _unitOfWork.GetRepository<SaberPro, int>();
                    if (!await saberProRepo.AnyAsync(x => x.Id == id, cancellationToken))
                        await saberProRepo.AddAsync(new SaberPro { Id = id, IdStateStage = targetStateStageId, IdUserCreatedAt = userId, OperationRegister = operationRegister, StatusRegister = true, CreatedAt = DateTimeOffset.UtcNow });
                    break;
            }
        }
    }
}

