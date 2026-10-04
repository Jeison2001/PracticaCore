using Domain.Common;
using Domain.Common.Inscriptions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Listado paginado de inscripciones con estudiantes. Filtra y pagina en la base de
    /// datos (WHERE antes de LIMIT/OFFSET) y carga los estudiantes solo de la página.
    /// </summary>
    public class InscriptionWithStudentsRepository : BaseRepository<InscriptionModality, int>, IInscriptionWithStudentsRepository
    {
        private new readonly AppDbContext _context;

        public InscriptionWithStudentsRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<InscriptionWithStudents>> GetFilteredPageAsync(
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false)
        {
            var remainingFilters = new Dictionary<string, string>();
            string? studentName = null, modalityName = null, stateName = null, periodCode = null;

            // Separar claves enriquecidas (se traducen a joins) del resto (FilterBuilder)
            if (filters != null)
            {
                foreach (var f in filters)
                {
                    var prop = f.Key.Contains('@') ? f.Key.Split('@', 2)[0] : f.Key;
                    switch (prop.ToLowerInvariant())
                    {
                        case "studentname": studentName = f.Value; break;
                        case "modalityname": modalityName = f.Value; break;
                        case "stateinscriptionname": stateName = f.Value; break;
                        case "academicperiodcode": periodCode = f.Value; break;
                        default: remainingFilters[f.Key] = f.Value; break;
                    }
                }
            }

            var query = _context.Set<InscriptionModality>().AsNoTracking().AsQueryable();

            // Filtros sobre propiedades directas (CreatedAt@ge/le, StatusRegister@eq, etc.)
            var entityFilter = FilterBuilder.BuildFilter<InscriptionModality, int>(remainingFilters);
            if (entityFilter != null)
                query = query.Where(entityFilter);

            // Claves enriquecidas → joins tipados (case-insensitive para texto libre)
            if (!string.IsNullOrWhiteSpace(studentName))
            {
                var pattern = LikePattern(studentName);
                query = query.Where(im => _context.Set<UserInscriptionModality>().Any(uim =>
                    uim.IdInscriptionModality == im.Id &&
                    uim.StatusRegister &&
                    uim.User != null &&
                    (EF.Functions.ILike(uim.User.FirstName + " " + uim.User.LastName, pattern) ||
                     EF.Functions.ILike(uim.User.FirstName, pattern) ||
                     EF.Functions.ILike(uim.User.LastName, pattern))));
            }

            if (!string.IsNullOrWhiteSpace(modalityName))
            {
                var pattern = LikePattern(modalityName);
                query = query.Where(im => im.Modality != null && EF.Functions.ILike(im.Modality.Name, pattern));
            }

            if (!string.IsNullOrWhiteSpace(stateName))
                query = query.Where(im => im.StateInscription != null && im.StateInscription.Name == stateName);

            if (!string.IsNullOrWhiteSpace(periodCode))
                query = query.Where(im => im.AcademicPeriod != null && im.AcademicPeriod.Code == periodCode);

            // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier
            // profundidad de página. El orden queda forzado a (CreatedAt DESC, Id DESC)
            // para que el cursor sea estable; se ignora el SortBy recibido.
            var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
            if (usingKeyset)
            {
                var anchorCreatedAt = cursorCreatedAt!.Value;
                query = query
                    .Where(im => im.CreatedAt < anchorCreatedAt ||
                                 (im.CreatedAt == anchorCreatedAt && im.Id < cursorId!.Value))
                    .OrderByDescending(im => im.CreatedAt)
                    .ThenByDescending(im => im.Id);
            }
            else
            {
                // Ordenamiento (lista blanca de campos de la entidad; por defecto CreatedAt desc)
                query = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
                {
                    "updatedat" => isDescending ? query.OrderByDescending(im => im.UpdatedAt) : query.OrderBy(im => im.UpdatedAt),
                    "id" => isDescending ? query.OrderByDescending(im => im.Id) : query.OrderBy(im => im.Id),
                    "approvaldate" => isDescending ? query.OrderByDescending(im => im.ApprovalDate) : query.OrderBy(im => im.ApprovalDate),
                    "createdat" => isDescending ? query.OrderByDescending(im => im.CreatedAt) : query.OrderBy(im => im.CreatedAt),
                    _ => query.OrderByDescending(im => im.CreatedAt),
                };
                // Desempate estable por Id (páginas deterministas con CreatedAt repetido)
                query = ((System.Linq.IOrderedQueryable<InscriptionModality>)query).ThenByDescending(im => im.Id);
            }

            // Conteo y página en la base de datos (sin traer tablas completas).
            // En keyset con SkipTotalCount se omite el COUNT (el frontend conserva el
            // total de la primera página): el COUNT con filtro escala igual de mal.
            var totalRecords = usingKeyset && skipTotalCount ? -1 : await query.CountAsync(cancellationToken);

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = Math.Max(totalRecords, 1); // contrato: PageSize <= 0 = sin paginación

            // Keyset trae pageSize+1 filas para detectar HasMoreRows sin ejecutar COUNT.
            var pagedQuery = usingKeyset
                ? query.Take(pageSize + 1)
                : query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var pageRows = await pagedQuery
                .Select(im => new
                {
                    Inscription = im,
                    AcademicPeriodCode = im.AcademicPeriod!.Code,
                    ModalityName = im.Modality!.Name,
                    StateInscriptionName = im.StateInscription!.Name,
                    StateInscriptionCode = im.StateInscription.Code,
                    StageModalityName = im.StageModality != null ? im.StageModality.Name : null,
                    StageOrder = im.StageModality != null ? (int?)im.StageModality.StageOrder : null,
                })
                .ToListAsync(cancellationToken);

            bool? hasMoreRows = null;
            if (usingKeyset)
            {
                hasMoreRows = pageRows.Count > pageSize;
                if (hasMoreRows.Value)
                    pageRows = pageRows.Take(pageSize).ToList();
            }

            // Estudiantes SOLO de la página (una sola consulta extra con la navegación User)
            var pageIds = pageRows.Select(r => r.Inscription.Id).ToList();
            var students = await _context.Set<UserInscriptionModality>()
                .Include(uim => uim.User)
                .AsNoTracking()
                .Where(uim => pageIds.Contains(uim.IdInscriptionModality))
                .ToListAsync(cancellationToken);

            var studentsByInscription = students
                .GroupBy(s => s.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = pageRows.Select(r => new InscriptionWithStudents
            {
                InscriptionModality = r.Inscription,
                AcademicPeriodCode = r.AcademicPeriodCode,
                ModalityName = r.ModalityName,
                StateInscriptionName = r.StateInscriptionName,
                StateInscriptionCode = r.StateInscriptionCode,
                StageModalityName = r.StageModalityName,
                StageOrder = r.StageOrder,
                Students = studentsByInscription.TryGetValue(r.Inscription.Id, out var list)
                    ? list
                    : new List<UserInscriptionModality>(),
            }).ToList();

            return new PaginatedResult<InscriptionWithStudents>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMoreRows = hasMoreRows,
            };
        }

        /// <summary>Patrón ILIKE con escapes básicos de comodines.</summary>
        private static string LikePattern(string value) =>
            "%" + value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }
}
