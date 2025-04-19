using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Application.Shared.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

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
            try
            {
                // 1. Actualizar el registro de modalidad
                var registerModalityDto = await _mediator.Send(
                    new UpdateEntityCommand<RegisterModality, int, RegisterModalityDto>(
                        request.Id, request.Dto.RegisterModality),
                    cancellationToken);

                // 2. Obtener todos los estudiantes actuales asociados a esta modalidad
                var currentStudentsQuery = new GetAllEntitiesQuery<RegisterModalityStudent, int, RegisterModalityStudentDto>
                {
                    Filters = new Dictionary<string, string>
                    {
                        { "IdRegisterModality", request.Id.ToString() }
                    }
                };
                
                var currentStudentsResult = await _mediator.Send(currentStudentsQuery, cancellationToken);
                var currentStudents = currentStudentsResult.Items.ToList();
                
                // 3. Identificar estudiantes a eliminar, actualizar o crear
                var studentIdsToKeep = request.Dto.Students
                    .Where(s => s.Id != 0)
                    .Select(s => s.Id)
                    .ToList();
                
                // Eliminar estudiantes que ya no están en la lista
                foreach (var student in currentStudents)
                {
                    if (!studentIdsToKeep.Contains(student.Id))
                    {
                        await _mediator.Send(
                            new DeleteEntityCommand<RegisterModalityStudent, int>(student.Id),
                            cancellationToken);
                    }
                }
                
                // 4. Actualizar o crear estudiantes
                foreach (var studentDto in request.Dto.Students)
                {
                    // Asegurarse de que el IdRegisterModality esté correctamente establecido
                    studentDto.IdRegisterModality = request.Id;
                    
                    if (studentDto.Id != 0)
                    {
                        // Actualizar estudiante existente
                        await _mediator.Send(
                            new UpdateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(
                                studentDto.Id, studentDto),
                            cancellationToken);
                    }
                    else
                    {
                        // Crear nuevo estudiante
                        var newStudent = await _mediator.Send(
                            new CreateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(studentDto),
                            cancellationToken);
                        
                        // Actualizar el ID en el DTO original
                        studentDto.Id = newStudent.Id;
                    }
                }
                
                // 5. Preparar y devolver la respuesta
                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
                    Students = request.Dto.Students
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar registro de modalidad con estudiantes");
                throw;
            }
        }
    }
}