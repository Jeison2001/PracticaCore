using Application.Shared.DTOs;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Queries
{
    public class GetDocumentsByInscriptionModalityQueryHandler : IRequestHandler<GetDocumentsByInscriptionModalityQuery, PaginatedResult<DocumentDto>>
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IMapper _mapper;

        public GetDocumentsByInscriptionModalityQueryHandler(IDocumentRepository documentRepository, IMapper mapper)
        {
            _documentRepository = documentRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<DocumentDto>> Handle(GetDocumentsByInscriptionModalityQuery request, CancellationToken cancellationToken)
        {
            var paginatedDocs = await _documentRepository.GetDocumentsByInscriptionModalityWithFiltersAsync(
                request.IdInscriptionModality,
                request.IdStageModality,
                request.IdDocumentClass,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                cancellationToken
            );

            var dtoResult = new PaginatedResult<DocumentDto>
            {
                Items = _mapper.Map<IEnumerable<DocumentDto>>(paginatedDocs.Items),
                TotalRecords = paginatedDocs.TotalRecords,
                PageNumber = paginatedDocs.PageNumber,
                PageSize = paginatedDocs.PageSize
            };

            return dtoResult;
        }
    }
}
