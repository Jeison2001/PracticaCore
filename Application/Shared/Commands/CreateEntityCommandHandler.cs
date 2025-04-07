using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands
{
    public class CreateEntityCommandHandler<T, TId, TDto> : IRequestHandler<CreateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEntityCommandHandler(IRepository<T, TId> repository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TDto> Handle(CreateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var entity = _mapper.Map<T>(request.Dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(ct);
            return _mapper.Map<TDto>(entity);
        }
    }
}
