using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries
{
    public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IMapper _mapper;

        public GetDocumentByIdQueryHandler(IRepository<Document, int> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetFirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
            return entity == null ? null : _mapper.Map<DocumentDto>(entity);
        }
    }
}
