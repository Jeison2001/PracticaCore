using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Registration;
using Domain.Interfaces.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    public class CreateInscriptionWithStudentsHandler : IRequestHandler<CreateInscriptionWithStudentsCommand, InscriptionWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateInscriptionWithStudentsHandler> _logger;
        private readonly IUserService _userService;
        private readonly IAcademicPeriodService _academicPeriodService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailNotificationQueueService _queueService;

        public CreateInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateInscriptionWithStudentsHandler> logger,
            IUserService userService,
            IAcademicPeriodService academicPeriodService,
            IUnitOfWork unitOfWork,
            IEmailNotificationQueueService queueService)
        {
            _mediator = mediator;
            _logger = logger;
            _userService = userService;
            _academicPeriodService = academicPeriodService;
            _unitOfWork = unitOfWork;
            _queueService = queueService;
        }        public async Task<InscriptionWithStudentsDto> Handle(
            CreateInscriptionWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            // Validación: Debe haber al menos un estudiante
            if (request.Dto.Students == null || !request.Dto.Students.Any())
                throw new InvalidOperationException("Se requiere al menos un estudiante para crear una inscripción de modalidad.");

            // Validación: No debe haber estudiantes repetidos (por identificación)
            var repeated = request.Dto.Students
                .GroupBy(s => new { s.Identification, s.IdIdentificationType })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.Identification)
                .ToList();
            if (repeated.Any())
                throw new InvalidOperationException($"No se puede repetir el estudiante con identificación: {string.Join(", ", repeated)}");            // Auto-detección del período académico si no se proporciona
            int academicPeriodId;
            if (request.Dto.InscriptionModality.IdAcademicPeriod.HasValue)
            {
                academicPeriodId = request.Dto.InscriptionModality.IdAcademicPeriod.Value;
                // Validar que el período académico existe
                var specifiedPeriod = await _academicPeriodService.GetAcademicPeriodByIdAsync(academicPeriodId);
                if (specifiedPeriod == null)
                    throw new InvalidOperationException($"No se encontró el período académico con ID {academicPeriodId}");
            }
            else
            {
                // Auto-detectar período académico activo
                var activePeriod = await _academicPeriodService.GetActiveAcademicPeriodAsync();
                if (activePeriod == null)
                    throw new InvalidOperationException("No hay un período académico activo y no se especificó uno explícitamente.");
                academicPeriodId = activePeriod.Id;
            }

            // Validación: Modalidad existente
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(request.Dto.InscriptionModality.IdModality);
            if (modality == null)
                throw new InvalidOperationException($"No se encontró la modalidad con ID {request.Dto.InscriptionModality.IdModality}");            // Validación: Cupo máximo de estudiantes
            var maxStudents = modality.MaxStudents;
            if (maxStudents > 0 && request.Dto.Students.Count > maxStudents)
                throw new InvalidOperationException($"La modalidad solo permite un máximo de {maxStudents} estudiantes.");

            // Obtener información de usuarios por identificación
            var userDictionary = new Dictionary<string, UserIdentificationResult>();
            foreach (var student in request.Dto.Students)
            {
                var identResult = await _userService.GetUserIdByIdentification(
                    student.IdIdentificationType,
                    student.Identification);
                
                // Validar existencia de usuario
                if (identResult == null || identResult.Id <= 0)
                    throw new KeyNotFoundException($"No se encontró el usuario con identificación '{student.Identification}'");
                
                userDictionary.Add(student.Identification, identResult);
            }

            // Validación: Todos los usuarios deben tener el rol 'STUDENT'
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

            // Validación: Verificar que los estudiantes no tengan inscripciones activas
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            
            foreach (var student in request.Dto.Students)
            {
                var userInfo = userDictionary[student.Identification];
                
                // Buscar todas las inscripciones del usuario
                var userInscriptions = await userInscriptionRepo.GetAllAsync(
                    ui => ui.IdUser == userInfo.Id && ui.StatusRegister);
                
                if (userInscriptions.Any())
                {
                    // Verificar si alguna de estas inscripciones tiene una modalidad activa
                    var inscriptionModalityIds = userInscriptions.Select(ui => ui.IdInscriptionModality).ToList();
                    var activeInscriptionModalities = await inscriptionModalityRepo.GetAllAsync(
                        im => inscriptionModalityIds.Contains(im.Id) && im.StatusRegister);
                    
                    if (activeInscriptionModalities.Any())
                    {
                        throw new InvalidOperationException($"El estudiante con identificación '{student.Identification}' ya tiene una inscripción activa registrada.");
                    }
                }
            }

            try
            {
                // 1. Create the modality record (sin IdStateInscription e IdStageModality - serán manejados por trigger)
                var inscriptionModalityDto = await _mediator.Send(
                    new CreateEntityCommand<InscriptionModality, int, InscriptionModalityDto>(
                        new InscriptionModalityDto
                        {
                            IdModality = request.Dto.InscriptionModality.IdModality,
                            IdAcademicPeriod = academicPeriodId,
                            Observations = request.Dto.InscriptionModality.Observations
                        }),
                    cancellationToken);

                // 2. Get the generated ID for the modality record
                var inscriptionModalityId = inscriptionModalityDto.Id;

                // 3. Crear los registros de estudiantes enlazados a la modalidad
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

                // 4. Prepare and return the response
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

                // 5. Enviar notificación automática después de crear exitosamente la inscripción
                try
                {
                    // Obtener información adicional para la notificación
                    var academicPeriod = await _academicPeriodService.GetAcademicPeriodByIdAsync(academicPeriodId);
                    
                    var studentsInfo = request.Dto.Students.Select(studentDto => 
                    {
                        var userInfo = userDictionary[studentDto.Identification];
                        var user = usersDict[userInfo.Id];
                        return new { 
                            Name = $"{user.FirstName} {user.LastName}",
                            Email = user.Email,
                            Identification = studentDto.Identification
                        };
                    }).ToList();

                    // Preparar datos del evento para INSCRIPTION_CREATED - solo campos necesarios
                    var eventData = new Dictionary<string, object>
                    {
                        // Datos básicos de la inscripción
                        ["InscriptionId"] = inscriptionModalityId,
                        ["ModalityName"] = modality.Name,
                        ["AcademicPeriod"] = academicPeriod?.Code ?? "N/A",
                        ["InscriptionDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        
                        // Información de estudiantes (agregada)
                        ["StudentsCount"] = studentsInfo.Count,
                        ["StudentNames"] = string.Join(", ", studentsInfo.Select(s => s.Name)),
                        ["StudentEmails"] = string.Join(", ", studentsInfo.Select(s => s.Email))
                    };

                    // Encolar evento de notificación para procesamiento asíncrono
                    var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);
                    
                    _logger.LogInformation("Notificación automática encolada para inscripción ID {InscriptionId} con JobId: {JobId}", 
                        inscriptionModalityId, jobId);
                }
                catch (Exception notificationEx)
                {
                    // Log del error pero no fallar el proceso principal
                    _logger.LogError(notificationEx, "Error al enviar notificación automática para inscripción ID {InscriptionId}", inscriptionModalityId);
                    // No re-lanzar la excepción para que la inscripción se complete exitosamente
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
