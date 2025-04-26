using Application.Shared.DTOs.RegisterModality;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Application.Shared.DTOs.RegisterModalityStudent;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Commands.RegisterModalityWithStudents.Handlers
{
    public class CreateRegisterModalityWithStudentsHandler : IRequestHandler<CreateRegisterModalityWithStudentsCommand, RegisterModalityWithStudentsDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateRegisterModalityWithStudentsHandler> _logger;
        private readonly IUserService _userService;

        public CreateRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateRegisterModalityWithStudentsHandler> logger,
            IUserService userService)
        {
            _mediator = mediator;
            _logger = logger;
            _userService = userService;
        }

        public async Task<RegisterModalityWithStudentsDto> Handle(
            CreateRegisterModalityWithStudentsCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Create the modality record
                var registerModalityDto = await _mediator.Send(
                    new CreateEntityCommand<RegisterModality, int, RegisterModalityDto>(
                        new RegisterModalityDto
                        {
                            IdModality = request.Dto.RegisterModality.IdModality,
                            IdStateInscription = request.Dto.RegisterModality.IdStateInscription,
                            IdAcademicPeriod = request.Dto.RegisterModality.IdAcademicPeriod,
                            Observations = request.Dto.RegisterModality.Observations
                        }),
                    cancellationToken);

                // 2. Get the generated ID for the modality record
                var registerModalityId = registerModalityDto.Id;

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
                        new CreateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(
                            new RegisterModalityStudentDto
                            {
                                IdRegisterModality = registerModalityId,
                                IdUser = userIdentification.Id,
                                UserName = userIdentification.UserName
                            }),
                        cancellationToken);
                }

                // 5. Prepare and return the response
                var students = request.Dto.Students.Select(s =>
                {
                    var userIdentification = userDictionary[s.Identification];
                    return new RegisterModalityStudentDto
                    {
                        IdRegisterModality = registerModalityId,
                        IdUser = userIdentification.Id,
                        UserName = userIdentification.UserName
                    };
                }).ToList();

                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
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
