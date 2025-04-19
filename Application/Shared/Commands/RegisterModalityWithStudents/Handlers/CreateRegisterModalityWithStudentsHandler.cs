using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.RegisterModalityWithStudents.Handlers
{
    public class CreateRegisterModalityWithStudentsHandler : IRequestHandler<CreateRegisterModalityWithStudentsCommand, RegisterModalityWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateRegisterModalityWithStudentsHandler> _logger;

        public CreateRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateRegisterModalityWithStudentsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<RegisterModalityWithStudentsDto> Handle(
            CreateRegisterModalityWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Crear el registro de modalidad
                var registerModalityDto = await _mediator.Send(
                    new CreateEntityCommand<RegisterModality, int, RegisterModalityDto>(request.Dto.RegisterModality),
                    cancellationToken);

                // 2. Obtener el ID generado para el registro de modalidad
                var registerModalityId = registerModalityDto.Id;

                // 3. Crear los registros de estudiantes vinculados a la modalidad
                foreach (var studentDto in request.Dto.Students)
                {
                    // Asegurarse de que el IdRegisterModality esté correctamente establecido
                    studentDto.IdRegisterModality = registerModalityId;

                    // Crear el registro de estudiante
                    await _mediator.Send(
                        new CreateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(studentDto),
                        cancellationToken);
                }

                // 4. Preparar y devolver la respuesta
                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
                    Students = request.Dto.Students
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear registro de modalidad con estudiantes");
                throw;
            }
        }
    }
}