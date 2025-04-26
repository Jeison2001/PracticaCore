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
using Domain.Interfaces;

namespace Application.Shared.Queries.RegisterModalityWithStudents.Handlers
{
    public class GetRegisterModalityWithStudentsByUserHandler : IRequestHandler<GetRegisterModalityWithStudentsByUserQuery, List<RegisterModalityWithStudentsResponseDto>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetRegisterModalityWithStudentsByUserHandler> _logger;
        private readonly IRepository<AcademicPeriod, int> _academicPeriodRepository;
        private readonly IRepository<Modality, int> _modalityRepository;
        private readonly IRepository<StateInscription, int> _stateInscriptionRepository;
        private readonly IRepository<User, int> _userRepository;
        private readonly IRepository<RegisterModalityStudent, int> _registerModalityStudentRepository;
        private readonly IRepository<RegisterModality, int> _registerModalityRepository;

        public GetRegisterModalityWithStudentsByUserHandler(
            IMediator mediator,
            ILogger<GetRegisterModalityWithStudentsByUserHandler> logger,
            IRepository<AcademicPeriod, int> academicPeriodRepository,
            IRepository<Modality, int> modalityRepository,
            IRepository<StateInscription, int> stateInscriptionRepository,
            IRepository<User, int> userRepository,
            IRepository<RegisterModalityStudent, int> registerModalityStudentRepository,
            IRepository<RegisterModality, int> registerModalityRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _stateInscriptionRepository = stateInscriptionRepository;
            _userRepository = userRepository;
            _registerModalityStudentRepository = registerModalityStudentRepository;
            _registerModalityRepository = registerModalityRepository;
        }

        public async Task<List<RegisterModalityWithStudentsResponseDto>> Handle(
            GetRegisterModalityWithStudentsByUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener todos los RegisterModalityStudent del usuario específico
                var studentRegistrations = await _registerModalityStudentRepository.GetAllAsync(
                    filter: rms => rms.IdUser == request.IdUser);

                if (!studentRegistrations.Any())
                {
                    return new List<RegisterModalityWithStudentsResponseDto>();
                }

                // 2. Obtener los IDs de las modalidades a las que está asociado el usuario
                var registerModalityIds = studentRegistrations.Select(sr => sr.IdRegisterModality).Distinct().ToList();

                // 3. Obtener todos los registros de modalidad correspondientes
                var registerModalities = await _registerModalityRepository.GetAllAsync(
                    filter: rm => registerModalityIds.Contains(rm.Id));

                // 4. Obtener todos los estudiantes asociados a estos registros de modalidad
                var allStudentRegistrations = await _registerModalityStudentRepository.GetAllAsync(
                    filter: rms => registerModalityIds.Contains(rms.IdRegisterModality));

                // 5. Agrupar estudiantes por IdRegisterModality
                var studentsByModality = allStudentRegistrations
                    .GroupBy(s => s.IdRegisterModality)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 6. Obtener información relacionada (períodos académicos, modalidades, estados)
                var academicPeriodsIds = registerModalities.Select(rm => rm.IdAcademicPeriod).Distinct().ToList();
                var modalitiesIds = registerModalities.Select(rm => rm.IdModality).Distinct().ToList();
                var statesIds = registerModalities.Select(rm => rm.IdStateInscription).Distinct().ToList();

                // Obtener entidades relacionadas
                var academicPeriods = await _academicPeriodRepository.GetAllAsync(
                    filter: ap => academicPeriodsIds.Contains(ap.Id));
                
                var modalities = await _modalityRepository.GetAllAsync(
                    filter: m => modalitiesIds.Contains(m.Id));
                
                var states = await _stateInscriptionRepository.GetAllAsync(
                    filter: s => statesIds.Contains(s.Id));

                // Diccionarios para acceso rápido
                var academicPeriodsDict = academicPeriods.ToDictionary(ap => ap.Id);
                var modalitiesDict = modalities.ToDictionary(m => m.Id);
                var statesDict = states.ToDictionary(s => s.Id);

                // 7. Obtener información de los usuarios para los nombres
                var userIds = allStudentRegistrations.Select(s => s.IdUser).Distinct().ToList();
                var users = new Dictionary<int, User>();

                foreach (var userId in userIds)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        users[userId] = user;
                    }
                }

                // 8. Construir la respuesta para cada modalidad
                var resultItems = new List<RegisterModalityWithStudentsResponseDto>();
                
                foreach (var registerModality in registerModalities)
                {
                    academicPeriodsDict.TryGetValue(registerModality.IdAcademicPeriod, out var academicPeriod);
                    modalitiesDict.TryGetValue(registerModality.IdModality, out var modality);
                    statesDict.TryGetValue(registerModality.IdStateInscription, out var stateInscription);

                    if (academicPeriod == null || modality == null || stateInscription == null)
                    {
                        _logger.LogWarning($"Datos faltantes para el registro de modalidad con ID {registerModality.Id}");
                        continue;
                    }

                    // Crear DTO para el registro de modalidad
                    var registerModalityDto = new RegisterModalityDto
                    {
                        Id = registerModality.Id,
                        IdModality = registerModality.IdModality,
                        IdStateInscription = registerModality.IdStateInscription,
                        IdAcademicPeriod = registerModality.IdAcademicPeriod,
                        Observations = registerModality.Observations,
                        UpdatedAt = registerModality.UpdatedAt,
                        StatusRegister = registerModality.StatusRegister
                    };

                    // Obtener estudiantes de esta modalidad
                    studentsByModality.TryGetValue(registerModality.Id, out var studentsForModality);
                    var studentDtos = new List<RegisterModalityStudentDto>();

                    if (studentsForModality != null)
                    {
                        foreach (var student in studentsForModality)
                        {
                            var studentDto = new RegisterModalityStudentDto
                            {
                                Id = student.Id,
                                IdRegisterModality = student.IdRegisterModality,
                                IdUser = student.IdUser,
                                UserName = "Usuario no encontrado",
                                UpdatedAt = student.UpdatedAt,
                                StatusRegister = student.StatusRegister
                            };

                            if (users.TryGetValue(student.IdUser, out var user))
                            {
                                studentDto.UserName = $"{user.FirstName} {user.LastName}";
                            }

                            studentDtos.Add(studentDto);
                        }
                    }

                    // Añadir la modalidad completa a la respuesta
                    resultItems.Add(new RegisterModalityWithStudentsResponseDto
                    {
                        RegisterModality = registerModalityDto,
                        AcademicPeriodCode = academicPeriod.Code,
                        ModalityName = modality.Name,
                        RegisterModalityStateName = stateInscription.Name,
                        Students = studentDtos
                    });
                }

                return resultItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros de modalidad por usuario: {Message}", ex.Message);
                throw;
            }
        }
    }
}