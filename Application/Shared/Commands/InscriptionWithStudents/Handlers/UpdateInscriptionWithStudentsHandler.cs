using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.Queries;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    public class UpdateInscriptionWithStudentsHandler : IRequestHandler<UpdateInscriptionWithStudentsCommand, InscriptionWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateInscriptionWithStudentsHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<UpdateInscriptionWithStudentsHandler> logger,
            IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<InscriptionWithStudentsDto> Handle(
            UpdateInscriptionWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            // Validaciones previas eliminadas. Se implementará nueva lógica de validación.

            try
            {
                // Log the DTO values for debugging
                _logger.LogInformation("Updating InscriptionModality with the following values: {@Dto}", request.Dto.InscriptionModality);

                // 1. Update the modality registration
                var inscriptionModalityDto = await _mediator.Send(
                    new UpdateEntityCommand<InscriptionModality, int, InscriptionModalityDto>(
                        request.Id,
                        new InscriptionModalityDto
                        {
                            IdModality = request.Dto.InscriptionModality.IdModality,
                            IdStateInscription = request.Dto.InscriptionModality.IdStateInscription,
                            IdAcademicPeriod = request.Dto.InscriptionModality.IdAcademicPeriod,
                            Observations = request.Dto.InscriptionModality.Observations,
                            StatusRegister = request.Dto.InscriptionModality.StatusRegister
                        }),
                    cancellationToken);

                // 2. Fetch all current students associated with this modality
                var currentStudentsQuery = new GetAllEntitiesQuery<UserInscriptionModality, int, UserInscriptionModalityDto>
                {
                    Filters = new Dictionary<string, string>
                    {
                        { "IdInscriptionModality", request.Id.ToString() }
                    }
                };

                var currentStudentsResult = await _mediator.Send(currentStudentsQuery, cancellationToken);
                var currentStudents = currentStudentsResult.Items.ToList();

                // 3. Identify students to delete, update, or create
                var studentIdsToKeep = request.Dto.Students
                    .Where(s => s.Id != 0)
                    .Select(s => s.Id)
                    .ToList();

                // Delete students no longer in the list
                foreach (var student in currentStudents)
                {
                    if (!studentIdsToKeep.Contains(student.Id))
                    {
                        await _mediator.Send(
                            new UpdateStatusEntityCommand<UserInscriptionModality, int>(
                                student.Id,
                                false, // StatusRegister (desactivado)
                                0, // IdUserUpdateAt (ajustar según contexto)
                                "Desactivado" // OperationRegister
                            ),
                            cancellationToken);
                    }
                }

                // Update or create students
                foreach (var studentDto in request.Dto.Students)
                {
                    if (studentDto.Id != 0)
                    {
                        // Update existing student
                        await _mediator.Send(
                            new UpdateEntityCommand<UserInscriptionModality, int, UserInscriptionModalityDto>(
                                studentDto.Id,
                                new UserInscriptionModalityDto
                                {
                                    IdInscriptionModality = request.Id,
                                    IdUser = studentDto.IdUser,
                                    StatusRegister = studentDto.StatusRegister
                                }),
                            cancellationToken);
                    }
                    else
                    {
                        // Create new student
                        var newStudent = await _mediator.Send(
                            new CreateEntityCommand<UserInscriptionModality, int, UserInscriptionModalityDto>(
                                new UserInscriptionModalityDto
                                {
                                    IdInscriptionModality = request.Id,
                                    IdUser = studentDto.IdUser,
                                    StatusRegister = studentDto.StatusRegister
                                }),
                            cancellationToken);

                        // Update the ID in the original DTO
                        studentDto.Id = newStudent.Id;
                    }
                }

                // 4. Prepare and return the response
                return new InscriptionWithStudentsDto
                {
                    InscriptionModality = inscriptionModalityDto,
                    Students = request.Dto.Students.Select(s => new UserInscriptionModalityDto
                    {
                        Id = s.Id,
                        IdInscriptionModality = request.Id,
                        IdUser = s.IdUser,
                        StatusRegister = s.StatusRegister
                    }).ToList()
                };
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating modality registration with students");
                throw;
            }
        }
    }
}