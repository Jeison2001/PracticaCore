using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<Document>> GetDocumentsByInscriptionModalityWithFiltersAsync(
            int idInscriptionModality,
            int? idStageModality,
            int? idDocumentClass,
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            CancellationToken cancellationToken)
        {
            IQueryable<Document> query = _context.Documents
                .Include(d => d.DocumentType)
                .ThenInclude(dt => dt.DocumentClass)
                .AsNoTracking()
                .Where(d => d.IdInscriptionModality == idInscriptionModality);

            if (idStageModality.HasValue)
            {
                query = query.Where(d => d.DocumentType != null && d.DocumentType.IdStageModality == idStageModality.Value);
            }
            if (idDocumentClass.HasValue)
            {
                query = query.Where(d => d.DocumentType != null && d.DocumentType.IdDocumentClass == idDocumentClass.Value);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                query = isDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, sortBy))
                    : query.OrderBy(e => EF.Property<object>(e, sortBy));
            }

            var totalRecords = await query.CountAsync(cancellationToken);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return new PaginatedResult<Document>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
