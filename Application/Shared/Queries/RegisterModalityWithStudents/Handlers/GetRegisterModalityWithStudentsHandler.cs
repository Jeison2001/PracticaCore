using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Common;

namespace Application.Shared.Queries.RegisterModalityWithStudents.Handlers
{
    public class GetRegisterModalityWithStudentsHandler : IRequestHandler<GetRegisterModalityWithStudentsQuery, RegisterModalityWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetRegisterModalityWithStudentsHandler> _logger;

        public GetRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<GetRegisterModalityWithStudentsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<RegisterModalityWithStudentsDto> Handle(
            GetRegisterModalityWithStudentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener el registro de modalidad utilizando Id como int
                var registerModalityQuery = new GetEntityByIdQuery<RegisterModality, int, RegisterModalityDto>(request.Id);
                var registerModalityDto = await _mediator.Send(registerModalityQuery, cancellationToken);

                if (registerModalityDto == null)
                {
                    throw new KeyNotFoundException($"No se encontró el registro de modalidad con ID {request.Id}");
                }

                // 2. Obtener todos los estudiantes asociados a esta modalidad
                var studentsQuery = new GetAllEntitiesQuery<RegisterModalityStudent, int, RegisterModalityStudentDto>
                {
                    Filters = new Dictionary<string, string>
                    {
                        { "IdRegisterModality", request.Id.ToString() }
                    }
                };
                
                var studentsResult = await _mediator.Send(studentsQuery, cancellationToken);
                
                // 3. Construir y devolver la respuesta
                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
                    Students = studentsResult.Items.ToList()
                };
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registro de modalidad con estudiantes: {Message}", ex.Message);
                throw;
            }
        }
    }
}