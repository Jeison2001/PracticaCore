using Application.Shared.DTOs.RegisterModality;
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

        public CreateRegisterModalityWithStudentsHandler(
            IMediator mediator,
            ILogger<CreateRegisterModalityWithStudentsHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
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
                            IdRegisterModalityState = request.Dto.RegisterModality.IdRegisterModalityState,
                            IdAcademicPeriod = request.Dto.RegisterModality.IdAcademicPeriod,
                            Observations = request.Dto.RegisterModality.Observations
                        }),
                    cancellationToken);

                // 2. Get the generated ID for the modality record
                var registerModalityId = registerModalityDto.Id;

                // 3. Create the student records linked to the modality
                foreach (var studentDto in request.Dto.Students)
                {
                    await _mediator.Send(
                        new CreateEntityCommand<RegisterModalityStudent, int, RegisterModalityStudentDto>(
                            new RegisterModalityStudentDto
                            {
                                IdRegisterModality = registerModalityId,
                                IdUser = studentDto.IdUser
                            }),
                        cancellationToken);
                }

                // 4. Prepare and return the response
                return new RegisterModalityWithStudentsDto
                {
                    RegisterModality = registerModalityDto,
                    Students = request.Dto.Students.Select(s => new RegisterModalityStudentDto
                    {
                        IdRegisterModality = registerModalityId,
                        IdUser = s.IdUser
                    }).ToList()
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
