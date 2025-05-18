using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Commands
{
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
            await _repository.AddRangeAsync(entities);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<List<TDto>>(entities);
        }
    }
}
