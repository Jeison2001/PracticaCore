using Application.Shared.DTOs;
using AutoMapper;
using Domain.Common.Dashboard;
using Domain.Entities;
using MediatR;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Commands.Handlers
{
    /// <summary>
    /// Handler genérico para creación batch de entidades. Crea múltiples entidades
    /// en una sola transacción. Valida que todos los DTOs sean válidos antes de insertar.
    /// </summary>
    public class CreateEntitiesCommandHandler<T, TId, TDto> : IRequestHandler<CreateEntitiesCommand<T, TId, TDto>, List<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEntitiesCommandHandler(IRepository<T, TId> repository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TDto>> Handle(CreateEntitiesCommand<T, TId, TDto> request, CancellationToken cancellationToken)
        {
            if (request.Dtos == null || request.Dtos.Count == 0)
                throw new ArgumentException("Debe proporcionar al menos un elemento.");

            var entities = _mapper.Map<List<T>>(request.Dtos);
            var nowUtc = DateTimeOffset.UtcNow;
            foreach (var entity in entities)
            {
                if (entity is BaseEntity<TId> baseEntity)
                {
                    baseEntity.IdUserCreatedAt = request.CurrentUser.UserId;
                    baseEntity.CreatedAt = nowUtc;
                    baseEntity.UpdatedAt = null;
                }

                switch (entity)
                {
                    case SaberPro saberPro:
                        saberPro.ExamDate = DashboardTimeHelper.NormalizeCalendarDateUtc(saberPro.ExamDate);
                        break;
                    case ScientificArticle scientificArticle:
                        scientificArticle.AcceptanceDate = DashboardTimeHelper.NormalizeCalendarDateUtc(scientificArticle.AcceptanceDate);
                        break;
                    case AcademicPractice academicPractice:
                        academicPractice.PracticeStartDate = DashboardTimeHelper.NormalizeCalendarDateUtc(academicPractice.PracticeStartDate);
                        academicPractice.PracticeEndDate = DashboardTimeHelper.NormalizeCalendarDateUtc(academicPractice.PracticeEndDate);
                        break;
                }
            }
            await _repository.AddRangeAsync(entities);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<List<TDto>>(entities);
        }
    }
}