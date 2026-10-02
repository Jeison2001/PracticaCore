using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.UserInscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.InscriptionWithStudents.Handlers
{
    public class GetInscriptionWithStudentsHandler : IRequestHandler<GetInscriptionWithStudentsQuery, InscriptionWithStudentsResponseDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GetInscriptionWithStudentsHandler> _logger;
        private readonly IRepository<AcademicPeriod, int> _academicPeriodRepository;
        private readonly IRepository<Modality, int> _modalityRepository;
        private readonly IRepository<StateInscription, int> _stateInscriptionRepository;
        private readonly IRepository<StageModality, int> _stageModalityRepository;
        private readonly IRepository<User, int> _userRepository;

        public GetInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<GetInscriptionWithStudentsHandler> logger,
            IRepository<AcademicPeriod, int> academicPeriodRepository,
            IRepository<Modality, int> modalityRepository,
            IRepository<StateInscription, int> stateInscriptionRepository,
            IRepository<StageModality, int> stageModalityRepository,
            IRepository<User, int> userRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _academicPeriodRepository = academicPeriodRepository;
            _modalityRepository = modalityRepository;
            _stateInscriptionRepository = stateInscriptionRepository;
            _stageModalityRepository = stageModalityRepository;
            _userRepository = userRepository;
        }

        public async Task<InscriptionWithStudentsResponseDto> Handle(
            GetInscriptionWithStudentsQuery request,
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
                
                // Obtener la etapa de modalidad si existe
                StageModality? stageModality = null;
                if (inscriptionModalityDto.IdStageModality.HasValue)
                {
                    stageModality = await _stageModalityRepository.GetByIdAsync(inscriptionModalityDto.IdStageModality.Value);
                }

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
                    PageSize = 0 // Contrato: PageSize <= 0 = sin paginación (los de esta inscripción)
                };

                var studentsResult = await _mediator.Send(studentsQuery, cancellationToken);

                // 4. Obtener los nombres de los estudiantes (una sola consulta por lote)
                var students = studentsResult.Items.ToList();
                var studentUserIds = students.Select(s => s.IdUser).Distinct().ToList();
                var usersById = (await _userRepository.GetAllAsync(u => studentUserIds.Contains(u.Id)))
                    .ToDictionary(u => u.Id);
                foreach (var student in students)
                {
                    usersById.TryGetValue(student.IdUser, out var user);
                    if (user == null) _logger.LogWarning("No se encontró el usuario con Id: {IdUser}", student.IdUser);
                    if (user != null)
                    {
                        student.UserName = $"{user.FirstName} {user.LastName}";
                        student.Identification = user.Identification ?? string.Empty;
                        student.IdIdentificationType = user.IdIdentificationType;
                        student.Email = user.Email ?? string.Empty;
                        student.CurrentAcademicPeriod = user.CurrentAcademicPeriod ?? string.Empty;
                        student.CumulativeAverage = user.CumulativeAverage;
                        student.ApprovedCredits = user.ApprovedCredits;
                        student.TotalAcademicCredits = user.TotalAcademicCredits;
                    }
                }

                // 5. Construir y devolver la respuesta
                return new InscriptionWithStudentsResponseDto
                {
                    InscriptionModality = inscriptionModalityDto,
                    AcademicPeriodCode = academicPeriod.Code, // Obtener el código del periodo académico
                    ModalityName = modality.Name, // Obtener el nombre de la modalidad
                    StateInscriptionName = stateInscription.Name, // Obtener el nombre del estado de inscripción
                    StateInscriptionCode = stateInscription.Code, // Código del estado de la inscripción
                    StageModalityName = stageModality?.Name, // Obtener el nombre de la etapa de modalidad
                    StageOrder = stageModality?.StageOrder, // Obtener el orden de la etapa
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