using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.UserInscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;
using Domain.Common.Users;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    /// <summary>
    /// Manejador principal para la creación de una nueva inscripción de modalidad.
    /// Valida cupos, asigna la fase inicial y propaga los eventos de dominio correspondientes.
    /// </summary>
    public class CreateInscriptionWithStudentsHandler : IRequestHandler<CreateInscriptionWithStudentsCommand, InscriptionWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateInscriptionWithStudentsHandler> _logger;
        private readonly IUserService _userService;
        private readonly IAcademicPeriodService _academicPeriodService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;

        public CreateInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateInscriptionWithStudentsHandler> logger,
            IUserService userService,
            IAcademicPeriodService academicPeriodService,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer)
        {
            _mediator = mediator;
            _logger = logger;
            _userService = userService;
            _academicPeriodService = academicPeriodService;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<InscriptionWithStudentsDto> Handle(
            CreateInscriptionWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Dto.Students == null || !request.Dto.Students.Any())
                throw new InvalidOperationException("Se requiere al menos un estudiante para crear una inscripción de modalidad.");

            var repeated = request.Dto.Students
                .GroupBy(s => new { s.Identification, s.IdIdentificationType })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.Identification)
                .ToList();

            if (repeated.Any())
                throw new InvalidOperationException($"No se puede repetir el estudiante con identificación: {string.Join(", ", repeated)}");

            int academicPeriodId;
            if (request.Dto.InscriptionModality.IdAcademicPeriod.HasValue)
            {
                academicPeriodId = request.Dto.InscriptionModality.IdAcademicPeriod.Value;
                var specifiedPeriod = await _academicPeriodService.GetAcademicPeriodByIdAsync(academicPeriodId);
                if (specifiedPeriod == null)
                    throw new InvalidOperationException($"No se encontró el período académico con ID {academicPeriodId}");
            }
            else
            {
                var activePeriod = await _academicPeriodService.GetActiveAcademicPeriodAsync();
                if (activePeriod == null)
                    throw new InvalidOperationException("No hay un período académico activo y no se especificó uno explícitamente.");
                academicPeriodId = activePeriod.Id;
            }

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(request.Dto.InscriptionModality.IdModality);
            
            if (modality == null)
                throw new InvalidOperationException($"No se encontró la modalidad con ID {request.Dto.InscriptionModality.IdModality}");

            var maxStudents = modality.MaxStudents;
            if (maxStudents > 0 && request.Dto.Students.Count > maxStudents)
                throw new InvalidOperationException($"La modalidad solo permite un máximo de {maxStudents} estudiantes.");

            var userDictionary = new Dictionary<string, UserIdentificationResult>();
            foreach (var student in request.Dto.Students)
            {
                var identResult = await _userService.GetUserIdByIdentification(
                    student.IdIdentificationType,
                    student.Identification);
                
                if (identResult == null || identResult.Id <= 0)
                    throw new KeyNotFoundException($"No se encontró el usuario con identificación '{student.Identification}'");
                
                userDictionary.Add(student.Identification, identResult);
            }

            var userIds = userDictionary.Values.Select(u => u.Id).ToList();
            var users = await _unitOfWork.GetRepository<User, int>()
                .GetAllAsync(u => userIds.Contains(u.Id));
            
            var usersDict = users.ToDictionary(u => u.Id);
            
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var userRoles = await userRoleRepo.GetAllAsync(ur => userIds.Contains(ur.IdUser));
            
            var roleRepo = _unitOfWork.GetRepository<Role, int>();
            var studentRole = await roleRepo.GetFirstOrDefaultAsync(r => r.Code == "STUDENT", cancellationToken);
            
            if (studentRole == null)
                throw new InvalidOperationException("No se encontró el rol 'STUDENT' en el sistema.");
            
            var studentRoleId = studentRole.Id;
            foreach (var student in request.Dto.Students)
            {
                var userInfo = userDictionary[student.Identification];
                var hasStudentRole = userRoles.Any(ur => ur.IdUser == userInfo.Id && ur.IdRole == studentRoleId);
                
                if (!usersDict.ContainsKey(userInfo.Id) || !hasStudentRole)
                {
                    throw new InvalidOperationException($"El usuario con identificación '{student.Identification}' no existe o no tiene el rol 'STUDENT'.");
                }
            }

            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            
            foreach (var student in request.Dto.Students)
            {
                var userInfo = userDictionary[student.Identification];
                
                var userInscriptions = await userInscriptionRepo.GetAllAsync(
                    ui => ui.IdUser == userInfo.Id && ui.StatusRegister);
                
                var inscriptionModalityIds = userInscriptions.Select(ui => ui.IdInscriptionModality).ToList();
                if (inscriptionModalityIds.Any())
                {
                    bool hasActiveInscription = await inscriptionModalityRepo.AnyAsync(
                        im => inscriptionModalityIds.Contains(im.Id) && im.StatusRegister,
                        cancellationToken);
                    
                    if (hasActiveInscription)
                    {
                        throw new InvalidOperationException($"El estudiante con identificación '{student.Identification}' ya tiene una inscripción activa registrada.");
                    }
                }
            }

            try
            {
                // Assign initial state depending on the modality's approval requirements
                var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();
                var targetStateCode = modality.RequiresApproval ? Domain.Constants.StateInscriptionCodes.Pendiente : Domain.Constants.StateInscriptionCodes.NoAplica;
                var initialState = await stateInscriptionRepo.GetFirstOrDefaultAsync(s => s.Code == targetStateCode, cancellationToken);
                
                if (initialState == null)
                    throw new InvalidOperationException($"No se encontró el estado inicial: {targetStateCode}");

                // 1. Create the inscription record
                var inscriptionModalityDto = await _mediator.Send(
                    new CreateEntityCommand<InscriptionModality, int, InscriptionModalityDto>(
                        new InscriptionModalityDto
                        {
                            IdModality = request.Dto.InscriptionModality.IdModality,
                            IdAcademicPeriod = academicPeriodId,
                            IdStateInscription = initialState.Id,
                            Observations = request.Dto.InscriptionModality.Observations
                        }),
                    cancellationToken);

                var inscriptionModalityId = inscriptionModalityDto.Id;

                // 2. Link students to the newly created inscription
                foreach (var studentDto in request.Dto.Students)
                {
                    var userIdentification = userDictionary[studentDto.Identification];
                    
                    await _mediator.Send(
                        new CreateEntityCommand<UserInscriptionModality, int, UserInscriptionModalityDto>(
                            new UserInscriptionModalityDto
                            {
                                IdInscriptionModality = inscriptionModalityId,
                                IdUser = userIdentification.Id,
                                UserName = userIdentification.UserName
                            }),
                        cancellationToken);
                }

                // 3. Dispatch initial domain event to trigger extension record creation
                var primaryStudentId = userDictionary.Values.First().Id;
                await _mediator.Publish(new Domain.Events.InscriptionStateChangedEvent(
                    InscriptionModalityId: inscriptionModalityId,
                    ModalityId: modality.Id,
                    NewStateInscriptionId: initialState.Id,
                    TriggeredByUserId: primaryStudentId
                ), cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                // 4. Prepare response
                var students = request.Dto.Students.Select(s =>
                {
                    var userIdentification = userDictionary[s.Identification];
                    return new UserInscriptionModalityDto
                    {
                        IdInscriptionModality = inscriptionModalityId,
                        IdUser = userIdentification.Id,
                        UserName = userIdentification.UserName
                    };
                }).ToList();

                var result = new InscriptionWithStudentsDto
                {
                    InscriptionModality = inscriptionModalityDto,
                    Students = students
                };

                // 5. Encolar job de notificación asíncrona (patrón consistente)
                try
                {
                    var studentIds = userDictionary.Values.Select(u => u.Id).ToList();
                    _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                        x.HandleInscriptionCreationAsync(
                            inscriptionModalityId,
                            request.Dto.InscriptionModality.IdModality,
                            academicPeriodId,
                            studentIds));

                    _logger.LogInformation("Job de notificación encolado para inscripción ID {InscriptionId}", inscriptionModalityId);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Error al encolar job de notificación para inscripción ID {InscriptionId}", inscriptionModalityId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating modality registration with students");
                throw;
            }
        }
    }
}
