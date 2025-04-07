using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries
{
    public class GetAllEntitiesQueryHandler<T, TId, TDto> : IRequestHandler<GetAllEntitiesQuery<T, TId, TDto>, IEnumerable<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;

        public GetAllEntitiesQueryHandler(IRepository<T, TId> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TDto>> Handle(GetAllEntitiesQuery<T, TId, TDto> request, CancellationToken ct)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TDto>>(entities);
        }
    }
}
