using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    public class CreateInscriptionWithStudentsHandler : IRequestHandler<CreateInscriptionWithStudentsCommand, InscriptionWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateInscriptionWithStudentsHandler> _logger;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateInscriptionWithStudentsHandler> logger,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _logger = logger;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<InscriptionWithStudentsDto> Handle(
            CreateInscriptionWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            // Validación: Debe haber al menos un estudiante
            if (request.Dto.Students == null || !request.Dto.Students.Any())
                throw new InvalidOperationException("Se requiere al menos un estudiante para crear una inscripción de modalidad.");

            // Validación: No debe haber estudiantes repetidos
            var repeated = request.Dto.Students.GroupBy(s => s.IdUser).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (repeated.Any())
                throw new InvalidOperationException($"No se puede repetir el estudiante con IdUser: {string.Join(", ", repeated)}");

            // Validación: Cupo máximo
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(request.Dto.InscriptionModality.IdModality);
            if (modality == null)
                throw new KeyNotFoundException($"No se encontró la modalidad con Id {request.Dto.InscriptionModality.IdModality}");
            if (modality.MaxStudents > 0 && request.Dto.Students.Count > modality.MaxStudents)
                throw new InvalidOperationException($"El número de estudiantes ({request.Dto.Students.Count}) excede el cupo máximo permitido ({modality.MaxStudents}) para la modalidad seleccionada.");

            try
            {
                // 1. Create the modality record
                var inscriptionModalityDto = await _mediator.Send(
                    new CreateEntityCommand<InscriptionModality, int, InscriptionModalityDto>(
                        new InscriptionModalityDto
                        {
                            IdModality = request.Dto.InscriptionModality.IdModality,
                            IdStateInscription = request.Dto.InscriptionModality.IdStateInscription,
                            IdAcademicPeriod = request.Dto.InscriptionModality.IdAcademicPeriod,
                            Observations = request.Dto.InscriptionModality.Observations
                        }),
                    cancellationToken);

                // 2. Get the generated ID for the modality record
                var inscriptionModalityId = inscriptionModalityDto.Id;

                // 3. Obtener secuencialmente las identificaciones de usuario para evitar concurrencia en DbContext
                var userDictionary = new Dictionary<string, UserIdentificationResult>();
                foreach (var s in request.Dto.Students)
                {
                    var identResult = await _userService.GetUserIdByIdentification(
                        s.IdIdentificationType,
                        s.Identification);
                    // Validar existencia de usuario
                    if (identResult == null || identResult.Id <= 0)
                        throw new KeyNotFoundException($"No se encontró el usuario con identificación '{s.Identification}'");
                    userDictionary.Add(s.Identification, identResult);
                }

                // 4. Cargar todos los usuarios de una vez para validación
                var userIds = userDictionary.Values.Select(x => x.Id).ToList();
                var users = await _unitOfWork.GetRepository<User, int>()
                    .GetAllAsync(u => userIds.Contains(u.Id));
                var usersDict = users.ToDictionary(u => u.Id);

                // 5. Crear los registros de estudiantes enlazados a la modalidad
                foreach (var studentDto in request.Dto.Students)
                {
                    var userIdentification = userDictionary[studentDto.Identification];
                    // Validar que el usuario exista en el diccionario
                    if (!usersDict.ContainsKey(userIdentification.Id))
                        throw new KeyNotFoundException($"Usuario con ID {userIdentification.Id} no existe en la base de datos.");
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

                // 6. Prepare and return the response
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

                return new InscriptionWithStudentsDto
                {
                    InscriptionModality = inscriptionModalityDto,
                    Students = students
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating modality registration with students");
                throw;
            }
        }
    }
}
