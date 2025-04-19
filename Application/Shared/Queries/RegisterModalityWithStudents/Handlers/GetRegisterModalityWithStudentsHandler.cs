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
using Domain.Interfaces; // Asegúrate de agregar esta línea

namespace Application.Shared.Queries.RegisterModalityWithStudents.Handlers
{
    public class GetRegisterModalityWithStudentsHandler : IRequestHandler<GetRegisterModalityWithStudentsQuery, RegisterModalityWithStudentsResponseDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetRegisterModalityWithStudentsHandler> _logger;
        private readonly IReadRepository<AcademicPeriod> _academicPeriodRepository; // Inyecta el repositorio de AcademicPeriod
        private readonly IReadRepository<Modality> _modalityRepository; // Inyecta el repositorio de Modality
        private readonly IReadRepository<RegisterModalityState> _registerModalityStateRepository; // Inyecta el repositorio de RegisterModalityState
        private readonly IReadRepository<User> _userRepository; // Inyecta el repositorio de User

        public GetRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<GetRegisterModalityWithStudentsHandler> logger,
            IReadRepository<AcademicPeriod> academicPeriodRepository, // Agrega el repositorio de AcademicPeriod al constructor
            IReadRepository<Modality> modalityRepository, // Agrega el repositorio de Modality al constructor
            IReadRepository<RegisterModalityState> registerModalityStateRepository, // Agrega el repositorio de RegisterModalityState al constructor
            IReadRepository<User> userRepository) // Agrega el repositorio de User al constructor
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _registerModalityStateRepository = registerModalityStateRepository;
            _userRepository = userRepository;
        }

        public async Task<RegisterModalityWithStudentsResponseDto> Handle(
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

                // 2. Obtener las entidades relacionadas
                var academicPeriod = await _academicPeriodRepository.GetByIdAsync(registerModalityDto.IdAcademicPeriod);
                var modality = await _modalityRepository.GetByIdAsync(registerModalityDto.IdModality);
                var registerModalityState = await _registerModalityStateRepository.GetByIdAsync(registerModalityDto.IdRegisterModalityState);

                if (academicPeriod == null || modality == null || registerModalityState == null)
                {
                    throw new KeyNotFoundException($"No se encontró el periodo académico, la modalidad o el estado de registro asociada al registro con ID {request.Id}");
                }

                // 3. Obtener todos los estudiantes asociados a esta modalidad
                var studentsQuery = new GetAllEntitiesQuery<RegisterModalityStudent, int, RegisterModalityStudentDto>
{
    Filters = new Dictionary<string, string>(),
    PageNumber = 1,
    PageSize = int.MaxValue // Obtiene todos los registros sin paginar
};

var studentsResult = await _mediator.Send(studentsQuery, cancellationToken);


                // 4. Obtener los nombres de los estudiantes
                var students = studentsResult.Items.ToList();
                foreach (var student in students)
                {
                    var user = await _userRepository.GetByIdAsync(student.IdUser);
if (user == null) _logger.LogWarning("No se encontró el usuario con Id: {IdUser}", student.IdUser);
                    if (user != null)
                    {
                        student.UserName = $"{user.FirstName} {user.LastName}";
                    }
                }

                // 5. Construir y devolver la respuesta
                return new RegisterModalityWithStudentsResponseDto
                {
                    RegisterModality = registerModalityDto,
                    AcademicPeriodCode = academicPeriod.Code, // Obtener el código del periodo académico
                    ModalityName = modality.Name, // Obtener el nombre de la modalidad
                    RegisterModalityStateName = registerModalityState.Name, // Obtener el nombre del estado de registro
                    Students = students
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