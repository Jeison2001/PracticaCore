using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Queries
{
    public class GetDocumentsByInscriptionModalityQueryHandler : IRequestHandler<GetDocumentsByInscriptionModalityQuery, List<DocumentDto>>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IMapper _mapper;

        public GetDocumentsByInscriptionModalityQueryHandler(IRepository<Document, int> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<DocumentDto>> Handle(GetDocumentsByInscriptionModalityQuery request, CancellationToken cancellationToken)
        {
            var docs = await _repository.GetAllAsync(d => d.IdInscriptionModality == request.IdInscriptionModality);
            return docs.Select(d => _mapper.Map<DocumentDto>(d)).ToList();
        }
    }
}
