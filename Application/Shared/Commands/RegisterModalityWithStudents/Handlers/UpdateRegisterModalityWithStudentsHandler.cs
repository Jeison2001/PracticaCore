using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Application.Shared.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Commands.RegisterModalityWithStudents.Handlers
{
    public class UpdateRegisterModalityWithStudentsHandler : IRequestHandler<UpdateRegisterModalityWithStudentsCommand, RegisterModalityWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateRegisterModalityWithStudentsHandler> _logger;

        public UpdateRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<UpdateRegisterModalityWithStudentsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<RegisterModalityWithStudentsDto> Handle(
            UpdateRegisterModalityWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            // **Validation:** Ensure all students belong to the RegisterModality being updated
            foreach (var studentDto in request.Dto.Students)
            {
                if (studentDto.Id != 0)
                {
                    // Fetch the existing student from the database
                    var existingStudentQuery = new GetEntityByIdQuery<RegisterModalityStudent, int, RegisterModalityStudentDto>(studentDto.Id);
                    var existingStudent = await _mediator.Send(existingStudentQuery, cancellationToken);

                    if (existingStudent == null)
                    {
                        throw new KeyNotFoundException($"Student with ID {studentDto.Id} not found");
                    }

                    if (existingStudent.IdRegisterModality != request.Id)
                    {
                        throw new InvalidOperationException($"Student with ID {studentDto.Id} belongs to another modality registration.");
                    }
                }
            }

            try
            {
                // 1. Update the modality registration
                var registerModalityDto = await _mediator.Send(
                    new UpdateEntityCommand<RegisterModality, int, RegisterModalityDto>(
                        request.Id,
                        new RegisterModalityDto
                        {
                            IdModality = request.Dto.RegisterModality.IdModality,
                            IdStateInscription = request.Dto.RegisterModality.IdStateInscription,
                            IdAcademicPeriod = request.Dto.RegisterModality.IdAcademicPeriod,
                            Observations = request.Dto.RegisterModality.Observations,
                            StatusRegister = request.Dto.RegisterModality.StatusRegister // Added StatusRegister
                        }),
                    cancellationToken);

                // 2. Fetch all current students associated with this modality
                var currentStudentsQuery = new GetAllEntitiesQuery<RegisterModalityStudent, int, RegisterModalityStudentDto>
                {
                    Filters = new Dictionary<string, string>
                    {
                        { "IdRegisterModality", request.Id.ToString() }
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
                            new DeleteEntityCommand<RegisterModalityStudent, int>(student.Id),
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
                            new UpdateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(
                                studentDto.Id,
                                new RegisterModalityStudentDto
                                {
                                    IdRegisterModality = request.Id,
                                    IdUser = studentDto.IdUser,
                                    StatusRegister = studentDto.StatusRegister // Added StatusRegister
                                }),
                            cancellationToken);
                    }
                    else
                    {
                        // Create new student
                        var newStudent = await _mediator.Send(
                            new CreateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(
                                new RegisterModalityStudentDto
                                {
                                    IdRegisterModality = request.Id,
                                    IdUser = studentDto.IdUser,
                                    StatusRegister = studentDto.StatusRegister // Added StatusRegister
                                }),
                            cancellationToken);

                        // Update the ID in the original DTO
                        studentDto.Id = newStudent.Id;
                    }
                }

                // 4. Prepare and return the response
                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
                    Students = request.Dto.Students.Select(s => new RegisterModalityStudentDto
                    {
                        Id = s.Id,
                        IdRegisterModality = request.Id,
                        IdUser = s.IdUser,
                        StatusRegister = s.StatusRegister // Added StatusRegister
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