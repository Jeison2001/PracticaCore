using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Commands.InscriptionWithStudents.Handlers
{
    public class CreateInscriptionWithStudentsHandler : IRequestHandler<CreateInscriptionWithStudentsCommand, InscriptionWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateInscriptionWithStudentsHandler> _logger;
        private readonly IUserService _userService;

        public CreateInscriptionWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateInscriptionWithStudentsHandler> logger,
            IUserService userService)
        {
            _mediator = mediator;
            _logger = logger;
            _userService = userService;
        }

        public async Task<InscriptionWithStudentsDto> Handle(
            CreateInscriptionWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
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

                // 3. Fetch all user identifications in a single step
                var userIdentifications = await Task.WhenAll(
                    request.Dto.Students.Select(async s => new
                    {
                        s.Identification,
                        s.IdIdentificationType,
                        UserIdentification = await _userService.GetUserIdByIdentification(s.IdIdentificationType, s.Identification)
                    })
                );

                var userDictionary = userIdentifications.ToDictionary(
                    x => x.Identification,
                    x => x.UserIdentification
                );

                // 4. Create the student records linked to the modality
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

                // 5. Prepare and return the response
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
