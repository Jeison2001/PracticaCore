using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries
{
    public class GetEntityByIdQueryHandler<T, TId, TDto> : IRequestHandler<GetEntityByIdQuery<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;

        public GetEntityByIdQueryHandler(IRepository<T, TId> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TDto> Handle(GetEntityByIdQuery<T, TId, TDto> request, CancellationToken ct)
        {
            var entity = await _repository.GetByIdAsync(request.Id) ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");
            return _mapper.Map<TDto>(entity);
        }
    }
}
