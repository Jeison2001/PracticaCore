using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Common;
using Domain.Interfaces;

namespace Application.Shared.Queries.RegisterModalityWithStudents.Handlers
{
    public class GetRegisterModalityWithStudentsHandler : IRequestHandler<GetRegisterModalityWithStudentsQuery, RegisterModalityWithStudentsResponseDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetRegisterModalityWithStudentsHandler> _logger;
        private readonly IRepository<AcademicPeriod, int> _academicPeriodRepository;
        private readonly IRepository<Modality, int> _modalityRepository;
        private readonly IRepository<StateInscription, int> _stateInscriptionRepository;
        private readonly IRepository<User, int> _userRepository;

        public GetRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<GetRegisterModalityWithStudentsHandler> logger,
            IRepository<AcademicPeriod, int> academicPeriodRepository,
            IRepository<Modality, int> modalityRepository,
            IRepository<StateInscription, int> stateInscriptionRepository,
            IRepository<User, int> userRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _stateInscriptionRepository = stateInscriptionRepository;
            _userRepository = userRepository;
        }

        public async Task<RegisterModalityWithStudentsResponseDto> Handle(
            GetRegisterModalityWithStudentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener el registro de modalidad utilizando Id como int
                var inscriptionModalityQuery = new GetEntityByIdQuery<InscriptionModality, int, InscriptionModalityDto>(request.Id);
                var inscriptionModalityDto = await _mediator.Send(inscriptionModalityQuery, cancellationToken);

                if (inscriptionModalityDto == null)
                {
                    throw new KeyNotFoundException($"No se encontró el registro de modalidad con ID {request.Id}");
                }

                // 2. Obtener las entidades relacionadas
                var academicPeriod = await _academicPeriodRepository.GetByIdAsync(inscriptionModalityDto.IdAcademicPeriod);
                var modality = await _modalityRepository.GetByIdAsync(inscriptionModalityDto.IdModality);
                var stateInscription = await _stateInscriptionRepository.GetByIdAsync(inscriptionModalityDto.IdStateInscription);

                if (academicPeriod == null || modality == null || stateInscription == null)
                {
                    throw new KeyNotFoundException($"No se encontró el periodo académico, la modalidad o el estado de inscripción asociada al registro con ID {request.Id}");
                }

                // 3. Obtener todos los estudiantes asociados a esta modalidad
                var studentsQuery = new GetAllEntitiesQuery<UserInscriptionModality, int, UserInscriptionModalityDto>
                {
                    Filters = new Dictionary<string, string>
                    {
                        { "IdInscriptionModality", request.Id.ToString() }
                    },
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
                    InscriptionModality = inscriptionModalityDto,
                    AcademicPeriodCode = academicPeriod.Code, // Obtener el código del periodo académico
                    ModalityName = modality.Name, // Obtener el nombre de la modalidad
                    StateInscriptionName = stateInscription.Name, // Obtener el nombre del estado de inscripción
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