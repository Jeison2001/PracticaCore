using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.Queries.InscriptionWithStudents;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    public class RespondInscriptionInvitationCommandHandler
        : IRequestHandler<RespondInscriptionInvitationCommand, InscriptionWithStudentsResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public RespondInscriptionInvitationCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<InscriptionWithStudentsResponseDto> Handle(
            RespondInscriptionInvitationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CurrentUser?.UserId is not int currentUserId || currentUserId <= 0)
                throw new ForbiddenAccessException("Usuario autenticado requerido para responder la invitación.");

            var inscriptionRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var inscription = await inscriptionRepo.GetByIdAsync(request.InscriptionModalityId);
            if (inscription == null || !inscription.StatusRegister)
                throw new KeyNotFoundException($"No se encontró la inscripción con ID {request.InscriptionModalityId}.");

            var stateRepo = _unitOfWork.GetRepository<StateInscription, int>();
            var state = await stateRepo.GetByIdAsync(inscription.IdStateInscription);
            if (state == null || state.IsFinalStateForStage || inscription.IsFinalRejection)
                throw new InvalidOperationException("La inscripción ya finalizó su revisión y no permite confirmar o rechazar participación.");

            var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var link = await uimRepo.GetFirstOrDefaultAsync(
                x => x.IdInscriptionModality == request.InscriptionModalityId &&
                     x.IdUser == currentUserId &&
                     x.StatusRegister,
                cancellationToken);

            if (link == null)
                throw new ForbiddenAccessException("No tienes una invitación activa en esta inscripción.");

            if (link.IsConfirmed)
                throw new InvalidOperationException("Tu participación en esta inscripción ya fue confirmada.");

            if (request.Accept)
            {
                var otherConfirmedLinks = await uimRepo.GetAllAsync(
                    ui => ui.IdUser == currentUserId &&
                          ui.StatusRegister &&
                          ui.IsConfirmed &&
                          ui.IdInscriptionModality != request.InscriptionModalityId);

                var otherInscriptionIds = otherConfirmedLinks
                    .Select(ui => ui.IdInscriptionModality)
                    .Distinct()
                    .ToList();

                if (otherInscriptionIds.Count > 0)
                {
                    var hasOtherActiveInscription = await inscriptionRepo.AnyAsync(
                        im => otherInscriptionIds.Contains(im.Id) &&
                              im.StatusRegister &&
                              !im.IsFinalRejection,
                        cancellationToken);

                    if (hasOtherActiveInscription)
                        throw new InvalidOperationException("Ya tienes una inscripción activa confirmada en otra modalidad.");
                }

                link.IsConfirmed = true;
                link.UpdatedAt = DateTimeOffset.UtcNow;
                link.IdUserUpdatedAt = currentUserId;
                link.OperationRegister = "CONFIRMED_BY_COSTUDENT";

                await uimRepo.UpdatePartialAsync(link, [
                    x => x.IsConfirmed,
                    x => x.UpdatedAt,
                    x => x.IdUserUpdatedAt,
                    x => x.OperationRegister
                ]);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            else
            {
                var hasOtherConfirmedStudent = await uimRepo.AnyAsync(
                    x => x.IdInscriptionModality == request.InscriptionModalityId &&
                         x.Id != link.Id &&
                         x.StatusRegister &&
                         x.IsConfirmed,
                    cancellationToken);

                if (!hasOtherConfirmedStudent)
                    throw new InvalidOperationException("No es posible rechazar la participación cuando no quedan otros estudiantes confirmados en la inscripción.");

                link.StatusRegister = false;
                link.UpdatedAt = DateTimeOffset.UtcNow;
                link.IdUserUpdatedAt = currentUserId;
                link.OperationRegister = "REJECTED_BY_COSTUDENT";

                await uimRepo.UpdatePartialAsync(link, [
                    x => x.StatusRegister,
                    x => x.UpdatedAt,
                    x => x.IdUserUpdatedAt,
                    x => x.OperationRegister
                ]);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            return await _mediator.Send(new GetInscriptionWithStudentsQuery(request.InscriptionModalityId), cancellationToken);
        }
    }
}
