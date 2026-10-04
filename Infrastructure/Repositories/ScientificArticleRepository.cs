using Domain.Common;
using Domain.Common.ScientificArticle;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ScientificArticleRepository : BaseRepository<ScientificArticle, int>, IScientificArticleRepository
    {
        public ScientificArticleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<ScientificArticleWithDetails?> GetWithDetailsAsync(int id)
        {
            var entity = await _context.ScientificArticles
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.AcademicPeriod)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StageModality)
                .Include(x => x.StateStage)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return null;

            return await PopulateDetails(entity);
        }

        public async Task<PaginatedResult<ScientificArticleWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false)
        {
            filters ??= new Dictionary<string, string>();
            var query = _context.ScientificArticles
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.AcademicPeriod)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StageModality)
                .Include(x => x.StateStage)
                .AsQueryable();

            query = query.ApplyFilters<ScientificArticle, int>(filters);
            query = ApplySpecificFilters(query, filters);

            // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier
            // profundidad de página. El orden queda forzado a (CreatedAt DESC, Id DESC)
            // para que el cursor sea estable; se ignora el SortBy recibido.
            var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
            if (usingKeyset)
            {
                var anchorCreatedAt = cursorCreatedAt!.Value;
                query = query
                    .Where(x => x.CreatedAt < anchorCreatedAt ||
                                (x.CreatedAt == anchorCreatedAt && x.Id < cursorId!.Value))
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id);
            }
            else
            {
                // Sorting
                query = (sortBy?.ToLower() ?? "default") switch
                {
                    "createdat" => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                    _ => isDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
                };
                // Desempate estable por Id (páginas deterministas con CreatedAt repetido)
                query = ((System.Linq.IOrderedQueryable<ScientificArticle>)query).ThenByDescending(x => x.Id);
            }

            // En keyset con SkipTotalCount se omite el COUNT (el frontend conserva
            // el total de la primera página): TotalRecords llega en -1.
            var totalCount = usingKeyset && skipTotalCount
                ? -1
                : await query.CountAsync(cancellationToken);
                // PageSize <= 0: sin paginación (entrega todo el resultado en una sola página)
                if (pageSize <= 0) { pageNumber = 1; pageSize = Math.Max(totalCount, 1); }
            // Keyset trae pageSize+1 filas para detectar HasMoreRows sin ejecutar COUNT.
            var items = usingKeyset
                ? await query.Take(pageSize + 1).ToListAsync(cancellationToken)
                : await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            bool? hasMoreRows = null;
            if (usingKeyset)
            {
                hasMoreRows = items.Count > pageSize;
                if (hasMoreRows.Value)
                    items = items.Take(pageSize).ToList();
            }

            var resultItems = new List<ScientificArticleWithDetails>();
            var detailsBatch = await ModalityDetailsBatch.LoadAsync(
                _context, items.Select(i => i.Id).ToList(), cancellationToken);
            foreach (var item in items)
            {
                resultItems.Add(PopulateDetails(item, detailsBatch));
            }

            return new PaginatedResult<ScientificArticleWithDetails>
            {
                Items = resultItems,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMoreRows = hasMoreRows
            };
        }

        public async Task<List<ScientificArticleWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default)
        {
            var inscriptionIds = _context.Set<UserInscriptionModality>()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality);

            var query = _context.ScientificArticles
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.AcademicPeriod)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StageModality)
                .Include(x => x.StateStage)
                .Where(x => inscriptionIds.Contains(x.Id));

            if (status.HasValue)
            {
                query = query.Where(x => x.StatusRegister == status.Value);
            }

            var items = await query.ToListAsync(cancellationToken);
            var resultItems = new List<ScientificArticleWithDetails>();
            var detailsBatch = await ModalityDetailsBatch.LoadAsync(
                _context, items.Select(i => i.Id).ToList(), cancellationToken);
            foreach (var item in items)
            {
                resultItems.Add(PopulateDetails(item, detailsBatch));
            }
            return resultItems;
        }

        public async Task<PaginatedResult<ScientificArticleWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false)
        {
            filters ??= new Dictionary<string, string>();
            var query = _context.ScientificArticles
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.AcademicPeriod)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StageModality)
                .Include(x => x.StateStage)
                .Where(x => x.InscriptionModality.TeachingAssignments.Any(ta => ta.IdTeacher == teacherId))
                .AsQueryable();

            query = query.ApplyFilters<ScientificArticle, int>(filters);
            query = ApplySpecificFilters(query, filters);

             // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier
             // profundidad de página. El orden queda forzado a (CreatedAt DESC, Id DESC)
             // para que el cursor sea estable; se ignora el SortBy recibido.
            var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
            if (usingKeyset)
            {
                var anchorCreatedAt = cursorCreatedAt!.Value;
                query = query
                    .Where(x => x.CreatedAt < anchorCreatedAt ||
                                (x.CreatedAt == anchorCreatedAt && x.Id < cursorId!.Value))
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id);
            }
            else
            {
                // Sorting
                query = (sortBy?.ToLower() ?? "default") switch
                {
                    "createdat" => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                    _ => isDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
                };
                // Desempate estable por Id (páginas deterministas con CreatedAt repetido)
                query = ((System.Linq.IOrderedQueryable<ScientificArticle>)query).ThenByDescending(x => x.Id);
            }

            // En keyset con SkipTotalCount se omite el COUNT (el frontend conserva
            // el total de la primera página): TotalRecords llega en -1.
            var totalCount = usingKeyset && skipTotalCount
                ? -1
                : await query.CountAsync(cancellationToken);
                // PageSize <= 0: sin paginación (entrega todo el resultado en una sola página)
                if (pageSize <= 0) { pageNumber = 1; pageSize = Math.Max(totalCount, 1); }
            // Keyset trae pageSize+1 filas para detectar HasMoreRows sin ejecutar COUNT.
            var items = usingKeyset
                ? await query.Take(pageSize + 1).ToListAsync(cancellationToken)
                : await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            bool? hasMoreRows = null;
            if (usingKeyset)
            {
                hasMoreRows = items.Count > pageSize;
                if (hasMoreRows.Value)
                    items = items.Take(pageSize).ToList();
            }

            var resultItems = new List<ScientificArticleWithDetails>();
            var detailsBatch = await ModalityDetailsBatch.LoadAsync(
                _context, items.Select(i => i.Id).ToList(), cancellationToken);
            foreach (var item in items)
            {
                resultItems.Add(PopulateDetails(item, detailsBatch));
            }

            return new PaginatedResult<ScientificArticleWithDetails>
            {
                Items = resultItems,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMoreRows = hasMoreRows
            };
        }

        private async Task<ScientificArticleWithDetails> PopulateDetails(ScientificArticle entity, CancellationToken cancellationToken = default)
        {
            var userInscriptionModalities = await _context.Set<UserInscriptionModality>()
                .Where(uim => uim.IdInscriptionModality == entity.Id)
                .Include(uim => uim.User)
                .ToListAsync(cancellationToken);

            var teachingAssignments = await _context.Set<TeachingAssignment>()
                .Where(ta => ta.IdInscriptionModality == entity.Id)
                .Include(ta => ta.Teacher)
                .Include(ta => ta.TypeTeachingAssignment)
                .ToListAsync(cancellationToken);

            var documents = await _context.Set<Document>()
                .Where(d => d.IdInscriptionModality == entity.Id)
                .Include(d => d.DocumentType)
                .ToListAsync(cancellationToken);

            return new ScientificArticleWithDetails
            {
                ScientificArticle = entity,
                InscriptionModality = entity.InscriptionModality,
                StateStage = entity.StateStage,
                StageModality = entity.InscriptionModality?.StageModality,
                Modality = entity.InscriptionModality?.Modality,
                StateInscription = entity.InscriptionModality?.StateInscription,
                AcademicPeriod = entity.InscriptionModality?.AcademicPeriod,
                UserInscriptionModalities = userInscriptionModalities,
                TeachingAssignments = teachingAssignments,
                Documents = documents
            };
        }

        /// <summary>Variante sin consultas: usa los detalles ya cargados de la página (anti N+1).</summary>
        private ScientificArticleWithDetails PopulateDetails(ScientificArticle entity, ModalityDetailsBatch batch)
        {
            var userInscriptionModalities = batch.GetUsers(entity.Id);
            var teachingAssignments = batch.GetAssignments(entity.Id);
            var documents = batch.GetDocuments(entity.Id);
            return new ScientificArticleWithDetails
            {
                ScientificArticle = entity,
                InscriptionModality = entity.InscriptionModality,
                StateStage = entity.StateStage,
                StageModality = entity.InscriptionModality?.StageModality,
                Modality = entity.InscriptionModality?.Modality,
                StateInscription = entity.InscriptionModality?.StateInscription,
                AcademicPeriod = entity.InscriptionModality?.AcademicPeriod,
                UserInscriptionModalities = userInscriptionModalities,
                TeachingAssignments = teachingAssignments,
                Documents = documents
            };
        }

        private IQueryable<ScientificArticle> ApplySpecificFilters(IQueryable<ScientificArticle> query, Dictionary<string, string> filters)
        {
            foreach (var filter in filters)
            {
                var key = filter.Key.ToLower();
                var value = filter.Value;

                switch (key)
                {
                    case "studentname":
                    case "studentname@like":
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            var pattern = LikePattern(value);
                            query = query.Where(x => _context.Set<UserInscriptionModality>().Any(uim =>
                                uim.IdInscriptionModality == x.Id &&
                                uim.StatusRegister &&
                                uim.User != null &&
                                (EF.Functions.ILike(uim.User.FirstName + " " + uim.User.LastName, pattern) ||
                                 EF.Functions.ILike(uim.User.FirstName, pattern) ||
                                 EF.Functions.ILike(uim.User.LastName, pattern) ||
                                 EF.Functions.ILike(uim.User.Identification, pattern))));
                        }
                        break;

                    case "academicperiodcode":
                    case "academicperiodcode@eq":
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            query = query.Where(x => x.InscriptionModality != null &&
                                                    x.InscriptionModality.AcademicPeriod != null &&
                                                    x.InscriptionModality.AcademicPeriod.Code == value);
                        }
                        break;
                }
            }
            return query;
        }

        /// <summary>Patrón ILIKE con escapes básicos de comodines.</summary>
        private static string LikePattern(string value) =>
            "%" + value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }
}
