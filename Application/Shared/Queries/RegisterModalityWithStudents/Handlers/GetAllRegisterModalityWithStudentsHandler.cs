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
    public class GetAllRegisterModalityWithStudentsHandler : IRequestHandler<GetAllRegisterModalityWithStudentsQuery, PaginatedResult<RegisterModalityWithStudentsResponseDto>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetAllRegisterModalityWithStudentsHandler> _logger;
        private readonly IRepository<AcademicPeriod, int> _academicPeriodRepository;
        private readonly IRepository<Modality, int> _modalityRepository;
        private readonly IRepository<StateInscription, int> _stateInscriptionRepository;
        private readonly IRepository<User, int> _userRepository;
        private readonly IRepository<InscriptionModality, int> _inscriptionModalityRepository;

        public GetAllRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<GetAllRegisterModalityWithStudentsHandler> logger,
            IRepository<AcademicPeriod, int> academicPeriodRepository,
            IRepository<Modality, int> modalityRepository,
            IRepository<StateInscription, int> stateInscriptionRepository,
            IRepository<User, int> userRepository,
            IRepository<InscriptionModality, int> inscriptionModalityRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _stateInscriptionRepository = stateInscriptionRepository;
            _userRepository = userRepository;
            _inscriptionModalityRepository = inscriptionModalityRepository;
        }

        public async Task<PaginatedResult<RegisterModalityWithStudentsResponseDto>> Handle(
            GetAllRegisterModalityWithStudentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Obtener todos los registros de modalidad con paginación y filtros
                var getAllModalitiesQuery = new GetAllEntitiesQuery<InscriptionModality, int, InscriptionModalityDto>
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    IsDescending = request.IsDescending,
                    Filters = request.Filters
                };

                var registerModalitiesResult = await _mediator.Send(getAllModalitiesQuery, cancellationToken);
                var registerModalities = registerModalitiesResult.Items.ToList();

                if (registerModalities.Count == 0)
                {
                    return new PaginatedResult<RegisterModalityWithStudentsResponseDto>
                    {
                        Items = new List<RegisterModalityWithStudentsResponseDto>(),
                        TotalRecords = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                // 2. Obtener los IDs de las modalidades para filtrar los estudiantes
                var registerModalitiesIds = registerModalities.Select(rm => rm.Id).ToList();

                // 3. Obtener todos los estudiantes asociados a las modalidades recuperadas
                var studentsQuery = new GetAllEntitiesQuery<UserInscriptionModality, int, UserInscriptionModalityDto>
                {
                    Filters = new Dictionary<string, string>(),
                    PageNumber = 1,
                    PageSize = int.MaxValue
                };

                var studentsResult = await _mediator.Send(studentsQuery, cancellationToken);
                var allStudents = studentsResult.Items
                    .Where(s => registerModalitiesIds.Contains(s.IdInscriptionModality))
                    .ToList();

                // 4. Agrupar estudiantes por IdInscriptionModality
                var studentsByModality = allStudents.GroupBy(s => s.IdInscriptionModality)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 5. Obtener información relacionada (períodos académicos, modalidades, estados)
                var academicPeriodsIds = registerModalities.Select(rm => rm.IdAcademicPeriod).Distinct().ToList();
                var modalitiesIds = registerModalities.Select(rm => rm.IdModality).Distinct().ToList();
                var statesIds = registerModalities.Select(rm => rm.IdStateInscription).Distinct().ToList();

                // Usar GetAllAsync con filtros para obtener las entidades relacionadas
                var academicPeriods = await _academicPeriodRepository.GetAllAsync(
                    filter: ap => academicPeriodsIds.Contains(ap.Id));
                
                var modalities = await _modalityRepository.GetAllAsync(
                    filter: m => modalitiesIds.Contains(m.Id));
                
                var states = await _stateInscriptionRepository.GetAllAsync(
                    filter: s => statesIds.Contains(s.Id));

                var academicPeriodsDict = academicPeriods.ToDictionary(ap => ap.Id);
                var modalitiesDict = modalities.ToDictionary(m => m.Id);
                var statesDict = states.ToDictionary(s => s.Id);

                // 6. Obtener información de los usuarios para los nombres
                var userIds = allStudents.Select(s => s.IdUser).Distinct().ToList();
                var users = new Dictionary<int, User>();
                
                foreach (var userId in userIds)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        users[userId] = user;
                    }
                }

                // 7. Construir la respuesta para cada modalidad
                var resultItems = new List<RegisterModalityWithStudentsResponseDto>();
                
                foreach (var inscriptionModalityDto in registerModalities)
                {
                    academicPeriodsDict.TryGetValue(inscriptionModalityDto.IdAcademicPeriod, out var academicPeriod);
                    modalitiesDict.TryGetValue(inscriptionModalityDto.IdModality, out var modality);
                    statesDict.TryGetValue(inscriptionModalityDto.IdStateInscription, out var stateInscription);

                    if (academicPeriod == null || modality == null || stateInscription == null)
                    {
                        _logger.LogWarning($"Datos faltantes para el registro de modalidad con ID {inscriptionModalityDto.Id}");
                        continue;
                    }

                    // Obtener estudiantes de esta modalidad
                    studentsByModality.TryGetValue(inscriptionModalityDto.Id, out var studentsForModality);
                    var studentDtos = studentsForModality ?? new List<UserInscriptionModalityDto>();

                    // Añadir nombres de estudiantes
                    foreach (var student in studentDtos)
                    {
                        if (users.TryGetValue(student.IdUser, out var user))
                        {
                            student.UserName = $"{user.FirstName} {user.LastName}";
                        }
                        else
                        {
                            _logger.LogWarning($"No se encontró el usuario con Id: {student.IdUser}");
                            student.UserName = "Usuario no encontrado";
                        }
                    }

                    // Añadir la modalidad completa a la respuesta
                    resultItems.Add(new RegisterModalityWithStudentsResponseDto
                    {
                        InscriptionModality = inscriptionModalityDto,
                        AcademicPeriodCode = academicPeriod.Code,
                        ModalityName = modality.Name,
                        StateInscriptionName = stateInscription.Name,
                        Students = studentDtos
                    });
                }

                // 8. Devolver el resultado paginado
                return new PaginatedResult<RegisterModalityWithStudentsResponseDto>
                {
                    Items = resultItems,
                    TotalRecords = registerModalitiesResult.TotalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros de modalidad con estudiantes: {Message}", ex.Message);
                throw;
            }
        }
    }
}