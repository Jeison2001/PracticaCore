using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;

namespace Application.Shared.Queries.InscriptionWithStudents.Handlers
{
    public class GetInscriptionWithStudentsByUserHandler : IRequestHandler<GetInscriptionWithStudentsByUserQuery, List<InscriptionWithStudentsResponseDto>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetInscriptionWithStudentsByUserHandler> _logger;
        private readonly IRepository<AcademicPeriod, int> _academicPeriodRepository;
        private readonly IRepository<Modality, int> _modalityRepository;
        private readonly IRepository<StateInscription, int> _stateInscriptionRepository;
        private readonly IRepository<User, int> _userRepository;
        private readonly IRepository<UserInscriptionModality, int> _userInscriptionModalityRepository;
        private readonly IRepository<InscriptionModality, int> _inscriptionModalityRepository;

        public GetInscriptionWithStudentsByUserHandler(
            IMediator mediator,
            ILogger<GetInscriptionWithStudentsByUserHandler> logger,
            IRepository<AcademicPeriod, int> academicPeriodRepository,
            IRepository<Modality, int> modalityRepository,
            IRepository<StateInscription, int> stateInscriptionRepository,
            IRepository<User, int> userRepository,
            IRepository<UserInscriptionModality, int> userInscriptionModalityRepository,
            IRepository<InscriptionModality, int> inscriptionModalityRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _stateInscriptionRepository = stateInscriptionRepository;
            _userRepository = userRepository;
            _userInscriptionModalityRepository = userInscriptionModalityRepository;
            _inscriptionModalityRepository = inscriptionModalityRepository;
        }

        public async Task<List<InscriptionWithStudentsResponseDto>> Handle(
            GetInscriptionWithStudentsByUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener todos los UserInscriptionModality del usuario específico
                var studentRegistrations = await _userInscriptionModalityRepository.GetAllAsync(
                    filter: rms => rms.IdUser == request.IdUser);

                if (!studentRegistrations.Any())
                {
                    return new List<InscriptionWithStudentsResponseDto>();
                }

                // 2. Obtener los IDs de las modalidades a las que está asociado el usuario
                var inscriptionModalityIds = studentRegistrations.Select(sr => sr.IdInscriptionModality).Distinct().ToList();

                // 3. Obtener todos los registros de modalidad correspondientes
                var inscriptionModalities = await _inscriptionModalityRepository.GetAllAsync(
                    filter: rm => inscriptionModalityIds.Contains(rm.Id));

                // 4. Obtener todos los estudiantes asociados a estos registros de modalidad
                var allStudentRegistrations = await _userInscriptionModalityRepository.GetAllAsync(
                    filter: rms => inscriptionModalityIds.Contains(rms.IdInscriptionModality));

                // 5. Agrupar estudiantes por IdInscriptionModality
                var studentsByModality = allStudentRegistrations
                    .GroupBy(s => s.IdInscriptionModality)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 6. Obtener información relacionada (períodos académicos, modalidades, estados)
                var academicPeriodsIds = inscriptionModalities.Select(rm => rm.IdAcademicPeriod).Distinct().ToList();
                var modalitiesIds = inscriptionModalities.Select(rm => rm.IdModality).Distinct().ToList();
                var statesIds = inscriptionModalities.Select(rm => rm.IdStateInscription).Distinct().ToList();

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
                var resultItems = new List<InscriptionWithStudentsResponseDto>();
                
                foreach (var inscriptionModality in inscriptionModalities)
                {
                    academicPeriodsDict.TryGetValue(inscriptionModality.IdAcademicPeriod, out var academicPeriod);
                    modalitiesDict.TryGetValue(inscriptionModality.IdModality, out var modality);
                    statesDict.TryGetValue(inscriptionModality.IdStateInscription, out var stateInscription);

                    if (academicPeriod == null || modality == null || stateInscription == null)
                    {
                        _logger.LogWarning($"Datos faltantes para el registro de modalidad con ID {inscriptionModality.Id}");
                        continue;
                    }

                    // Crear DTO para el registro de modalidad
                    var inscriptionModalityDto = new InscriptionModalityDto
                    {
                        Id = inscriptionModality.Id,
                        IdModality = inscriptionModality.IdModality,
                        IdStateInscription = inscriptionModality.IdStateInscription,
                        IdAcademicPeriod = inscriptionModality.IdAcademicPeriod,
                        Observations = inscriptionModality.Observations,
                        UpdatedAt = inscriptionModality.UpdatedAt,
                        StatusRegister = inscriptionModality.StatusRegister
                    };

                    // Obtener estudiantes de esta modalidad
                    studentsByModality.TryGetValue(inscriptionModality.Id, out var studentsForModality);
                    var studentDtos = new List<UserInscriptionModalityDto>();

                    if (studentsForModality != null)
                    {
                        foreach (var student in studentsForModality)
                        {
                            var studentDto = new UserInscriptionModalityDto
                            {
                                Id = student.Id,
                                IdInscriptionModality = student.IdInscriptionModality,
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
                    resultItems.Add(new InscriptionWithStudentsResponseDto
                    {
                        InscriptionModality = inscriptionModalityDto,
                        AcademicPeriodCode = academicPeriod.Code,
                        ModalityName = modality.Name,
                        StateInscriptionName = stateInscription.Name,
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