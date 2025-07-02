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

        public async Task<List<RequiredDocumentsByState>> GetRequiredDocumentsByCurrentStateAsync(
            int inscriptionModalityId,
            CancellationToken cancellationToken)
        {
            // Obtener el estado actual de la práctica académica
            var academicPractice = await _context.AcademicPractices
                .Where(ap => ap.Id == inscriptionModalityId && ap.StatusRegister == true)
                .Select(ap => new { ap.Id, ap.IdStateStage })
                .FirstOrDefaultAsync(cancellationToken);

            if (academicPractice == null)
            {
                throw new KeyNotFoundException($"No se encontró una práctica académica activa para la inscripción {inscriptionModalityId}");
            }

            // Consultar los documentos requeridos para el estado actual con todos los includes
            var requiredDocuments = await _context.RequiredDocumentsByStates
                .Include(rds => rds.DocumentType)
                    .ThenInclude(dt => dt.DocumentClass)
                .Include(rds => rds.StateStage)
                .Where(rds => rds.IdStateStage == academicPractice.IdStateStage 
                           && rds.StatusRegister == true
                           && rds.DocumentType.StatusRegister == true)
                .OrderBy(rds => rds.OrderDisplay)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return requiredDocuments;
        }
    }
}
