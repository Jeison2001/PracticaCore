using Domain.Common;
using Domain.Common.SaberPro;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SaberProRepository : BaseRepository<SaberPro, int>, ISaberProRepository
    {
        public SaberProRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<SaberProWithDetails?> GetWithDetailsAsync(int id)
        {
            var entity = await _context.SaberPros
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

        public async Task<PaginatedResult<SaberProWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default)
        {
            filters ??= new Dictionary<string, string>();
            var query = _context.SaberPros
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

            query = query.ApplyFilters<SaberPro, int>(filters);
            query = ApplySpecificFilters(query, filters);

            // Sorting
            query = (sortBy?.ToLower() ?? "default") switch
            {
                "createdat" => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _ => isDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            var resultItems = new List<SaberProWithDetails>();
            foreach (var item in items)
            {
                resultItems.Add(await PopulateDetails(item, cancellationToken));
            }

            return new PaginatedResult<SaberProWithDetails>
            {
                Items = resultItems,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<SaberProWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default)
        {
            var inscriptionIds = _context.Set<UserInscriptionModality>()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality);

            var query = _context.SaberPros
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
            var resultItems = new List<SaberProWithDetails>();
            foreach (var item in items)
            {
                resultItems.Add(await PopulateDetails(item, cancellationToken));
            }

            return resultItems;
        }

        public async Task<PaginatedResult<SaberProWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default)
        {
            filters ??= new Dictionary<string, string>();
            var query = _context.SaberPros
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.AcademicPeriod)
                .Include(x => x.InscriptionModality)
                    .ThenInclude(im => im.StageModality)
                .Include(x => x.StateStage)
                .Where(x => x.InscriptionModality.TeachingAssignments.Any(ta => ta.IdTeacher == teacherId));

            query = query.ApplyFilters<SaberPro, int>(filters);
            query = ApplySpecificFilters(query, filters);

            // Sorting
            query = (sortBy?.ToLower() ?? "default") switch
            {
                "createdat" => isDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _ => isDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            var resultItems = new List<SaberProWithDetails>();
            foreach (var item in items)
            {
                resultItems.Add(await PopulateDetails(item, cancellationToken));
            }

            return new PaginatedResult<SaberProWithDetails>
            {
                Items = resultItems,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private async Task<SaberProWithDetails> PopulateDetails(SaberPro entity, CancellationToken cancellationToken = default)
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

            return new SaberProWithDetails
            {
                SaberPro = entity,
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

        private IQueryable<SaberPro> ApplySpecificFilters(IQueryable<SaberPro> query, Dictionary<string, string> filters)
        {
            foreach (var filter in filters)
            {
                var key = filter.Key.ToLower();
                var value = filter.Value;

                switch (key)
                {
                    case "idstateinscription":
                    case "idstateinscription@eq":
                        if (int.TryParse(value, out int stateInscriptionId))
                        {
                            query = query.Where(x => x.InscriptionModality != null && 
                                                    x.InscriptionModality.IdStateInscription == stateInscriptionId);
                        }
                        break;

                    case "idacademicperiod":
                    case "idacademicperiod@eq":
                        if (int.TryParse(value, out int academicPeriodId))
                        {
                            query = query.Where(x => x.InscriptionModality != null && 
                                                    x.InscriptionModality.IdAcademicPeriod == academicPeriodId);
                        }
                        break;

                    case "idstatestage":
                    case "idstatestage@eq":
                        if (int.TryParse(value, out int stateStageId))
                        {
                            query = query.Where(x => x.IdStateStage == stateStageId);
                        }
                        break;
                }
            }
            return query;
        }
    }
}
