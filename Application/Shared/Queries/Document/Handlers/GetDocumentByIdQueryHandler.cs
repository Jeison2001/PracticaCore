using Application.Shared.DTOs.Document;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Queries.Document.Handlers
{
    public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
    {
        private readonly IRepository<Domain.Entities.Document, int> _repository;
        private readonly IMapper _mapper;

        public GetDocumentByIdQueryHandler(
            IRepository<Domain.Entities.Document, int> repository,
            IMapper mapper)
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
