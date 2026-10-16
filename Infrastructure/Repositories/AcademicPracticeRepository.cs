using Domain.Entities;
using Domain.Common;
using Domain.Common.Teachers;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces.Repositories;
using Domain.Common.AcademicPractice;

namespace Infrastructure.Repositories;

public class AcademicPracticeRepository : BaseRepository<AcademicPractice, int>, IAcademicPracticeRepository
{
    public AcademicPracticeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<AcademicPracticeWithDetails?> GetAcademicPracticeWithDetailsAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var academicPractice = await _context.AcademicPractices
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.Modality)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StateInscription)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.AcademicPeriod)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StageModality)
            .Include(ap => ap.StateStage)
            .FirstOrDefaultAsync(ap => ap.Id == id, cancellationToken);

        if (academicPractice == null) return null;

        // Get related data
        var userInscriptionModalities = await _context.Set<UserInscriptionModality>()
            .Where(uim => uim.IdInscriptionModality == academicPractice.Id)
            .Include(uim => uim.User)
            .ToListAsync(cancellationToken);

        var teachingAssignments = await _context.Set<TeachingAssignment>()
            .Where(ta => ta.IdInscriptionModality == academicPractice.Id)
            .Include(ta => ta.Teacher)
            .Include(ta => ta.TypeTeachingAssignment)
            .ToListAsync(cancellationToken);

        var teachersByInscription = await TeacherAssignmentLoader
            .LoadActiveByInscriptionIdsAsync(_context, new List<int> { academicPractice.Id }, cancellationToken);

        var documents = await _context.Set<Document>()
            .Where(d => d.IdInscriptionModality == academicPractice.Id)
            .Include(d => d.DocumentType)
            .ToListAsync(cancellationToken);

        return new AcademicPracticeWithDetails
        {
            AcademicPractice = academicPractice,
            InscriptionModality = academicPractice.InscriptionModality,
            StateStage = academicPractice.StateStage,
            StageModality = academicPractice.InscriptionModality?.StageModality,
            Modality = academicPractice.InscriptionModality?.Modality,
            StateInscription = academicPractice.InscriptionModality?.StateInscription,
            AcademicPeriod = academicPractice.InscriptionModality?.AcademicPeriod,
            UserInscriptionModalities = userInscriptionModalities,
            TeachingAssignments = teachingAssignments,
            Teachers = teachersByInscription.TryGetValue(academicPractice.Id, out var teachers)
                ? teachers
                : new List<AssignedTeacher>(),
            Documents = documents
        };
    }

    public async Task<PaginatedResult<AcademicPracticeWithDetails>> GetAllAcademicPracticesWithDetailsPaginatedAsync(
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
        filters ??= new Dictionary<string, string>(); // Previene null reference
        var query = _context.AcademicPractices
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.Modality)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StateInscription)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.AcademicPeriod)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StageModality)
            .Include(ap => ap.StateStage)
            .AsQueryable();

        // Extraer filtros por docente/cargo
        var tf = TeacherAssignmentLoader.ExtractTeacherFilters(filters);
        var remainingFilters = tf.Remaining;
        if (tf.HasAny)
        {
            var isInMemory = _context.Database.ProviderName?.Contains("InMemory") == true
                || Environment.GetEnvironmentVariable("UseInMemoryDatabase") == "true";

            var teacherId = tf.Id;
            var cargoId = tf.CargoId;
            var cargoCode = tf.CargoCode;
            var cargoName = tf.CargoName;

            if (isInMemory)
            {
                var rawName = tf.Name?.Trim().ToLowerInvariant();
                var rawEmail = tf.Email?.Trim().ToLowerInvariant();
                query = query.Where(x => _context.Set<TeachingAssignment>().Any(ta =>
                    ta.IdInscriptionModality == x.Id && ta.StatusRegister && ta.RevocationDate == null &&
                    (rawName == null || (ta.Teacher != null &&
                        ((ta.Teacher.FirstName + " " + ta.Teacher.LastName).ToLower().Contains(rawName) ||
                         ta.Teacher.FirstName.ToLower().Contains(rawName) ||
                         ta.Teacher.LastName.ToLower().Contains(rawName)))) &&
                    (rawEmail == null || (ta.Teacher != null && ta.Teacher.Email.ToLower().Contains(rawEmail))) &&
                    (teacherId == null || ta.IdTeacher == teacherId) &&
                    (cargoId == null || ta.IdTypeTeachingAssignment == cargoId) &&
                    (cargoCode == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Code == cargoCode)) &&
                    (cargoName == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Name == cargoName))));
            }
            else
            {
                var namePattern = tf.Name != null ? TeacherAssignmentLoader.LikePattern(tf.Name) : null;
                var emailPattern = tf.Email != null ? TeacherAssignmentLoader.LikePattern(tf.Email) : null;
                query = query.Where(x => _context.Set<TeachingAssignment>().Any(ta =>
                    ta.IdInscriptionModality == x.Id && ta.StatusRegister && ta.RevocationDate == null &&
                    (namePattern == null || (ta.Teacher != null &&
                        (EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.FirstName + " " + ta.Teacher.LastName), namePattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.FirstName), namePattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.LastName), namePattern)))) &&
                    (emailPattern == null || (ta.Teacher != null && EF.Functions.ILike(ta.Teacher.Email, emailPattern))) &&
                    (teacherId == null || ta.IdTeacher == teacherId) &&
                    (cargoId == null || ta.IdTypeTeachingAssignment == cargoId) &&
                    (cargoCode == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Code == cargoCode)) &&
                    (cargoName == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Name == cargoName))));
            }
        }

        // Usar el sistema genérico de filtros en lugar de filtros manuales
        query = query.ApplyFilters<AcademicPractice, int>(remainingFilters);

        // Aplicar filtros específicos para entidades relacionadas
        query = ApplySpecificFilters(query, remainingFilters);

        // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier
        // profundidad de página. El orden queda forzado a (CreatedAt DESC, Id DESC)
        // para que el cursor sea estable; se ignora el SortBy recibido.
        var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
        if (usingKeyset)
        {
            var anchorCreatedAt = cursorCreatedAt!.Value;
            query = query
                .Where(ap => ap.CreatedAt < anchorCreatedAt ||
                             (ap.CreatedAt == anchorCreatedAt && ap.Id < cursorId!.Value))
                .OrderByDescending(ap => ap.CreatedAt)
                .ThenByDescending(ap => ap.Id);
        }
        else
        {
            // Apply sorting
            query = (sortBy?.ToLower() ?? "defaultSortField") switch
            {
                "createdat" => isDescending ? query.OrderByDescending(ap => ap.CreatedAt) : query.OrderBy(ap => ap.CreatedAt),
                "startdate" => isDescending ? query.OrderByDescending(ap => ap.PracticeStartDate) : query.OrderBy(ap => ap.PracticeStartDate),
                "institution" => isDescending ? query.OrderByDescending(ap => ap.InstitutionName) : query.OrderBy(ap => ap.InstitutionName),
                _ => query.OrderByDescending(ap => ap.CreatedAt)
            };
            // Desempate estable por Id (páginas deterministas con CreatedAt repetido)
            query = ((System.Linq.IOrderedQueryable<AcademicPractice>)query).ThenByDescending(ap => ap.Id);
        }

        // En keyset con SkipTotalCount se omite el COUNT (el frontend conserva el
        // total de la primera página): el COUNT con filtro escala igual de mal.
        var totalCount = usingKeyset && skipTotalCount ? -1 : await query.CountAsync(cancellationToken);
        // PageSize <= 0: sin paginación (entrega todo el resultado en una sola página)
        if (pageSize <= 0) { pageNumber = 1; pageSize = Math.Max(totalCount, 1); }
        // Keyset trae pageSize+1 filas para detectar HasMoreRows sin ejecutar COUNT.
        var pagedQuery = usingKeyset
            ? query.Take(pageSize + 1)
            : query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        var academicPractices = await pagedQuery.ToListAsync(cancellationToken);

        bool? hasMoreRows = null;
        if (usingKeyset)
        {
            hasMoreRows = academicPractices.Count > pageSize;
            if (hasMoreRows.Value)
                academicPractices = academicPractices.Take(pageSize).ToList();
        }

        // Get academic practice IDs for efficient student loading
        var academicPracticeIds = academicPractices.Select(ap => ap.Id).ToList();
        
        // Load students for these specific academic practices
        var userInscriptionModalities = await _context.Set<UserInscriptionModality>()
            .Where(uim => academicPracticeIds.Contains(uim.IdInscriptionModality))
            .Include(uim => uim.User)
            .ToListAsync(cancellationToken);

        // Load active teachers for these specific academic practices
        var teachersByInscription = await TeacherAssignmentLoader
            .LoadActiveByInscriptionIdsAsync(_context, academicPracticeIds, cancellationToken);

        // Convert to detailed results with students and teachers
        var items = academicPractices.Select(ap => new AcademicPracticeWithDetails
        {
            AcademicPractice = ap,
            InscriptionModality = ap.InscriptionModality,
            StateStage = ap.StateStage,
            StageModality = ap.InscriptionModality?.StageModality,
            Modality = ap.InscriptionModality?.Modality,
            StateInscription = ap.InscriptionModality?.StateInscription,
            AcademicPeriod = ap.InscriptionModality?.AcademicPeriod,
            UserInscriptionModalities = userInscriptionModalities.Where(uim => uim.IdInscriptionModality == ap.Id).ToList(),
            TeachingAssignments = new List<TeachingAssignment>(),
            Teachers = teachersByInscription.TryGetValue(ap.Id, out var teachers)
                ? teachers
                : new List<AssignedTeacher>(),
            Documents = new List<Document>()
        }).ToList();

        return new PaginatedResult<AcademicPracticeWithDetails>
        {
            Items = items,
            TotalRecords = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMoreRows = hasMoreRows
        };
    }

    public async Task<PaginatedResult<AcademicPracticeWithDetails>> GetAcademicPracticesByTeacherWithDetailsPaginatedAsync(
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
        filters ??= new Dictionary<string, string>(); // Previene null reference
        // Get academic practices assigned to a teacher through active teaching assignments
        var academicPracticeIds = await _context.Set<TeachingAssignment>()
            .Where(ta => ta.IdTeacher == teacherId && ta.StatusRegister && ta.RevocationDate == null)
            .Select(ta => ta.IdInscriptionModality)
            .ToListAsync(cancellationToken);

        var query = _context.AcademicPractices
            .Where(ap => academicPracticeIds.Contains(ap.Id))
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.Modality)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StateInscription)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.AcademicPeriod)
            .Include(ap => ap.StateStage)
            .AsQueryable();

        // Usar el sistema genérico de filtros en lugar de filtros manuales
        query = query.ApplyFilters<AcademicPractice, int>(filters);

        // Aplicar filtros específicos para entidades relacionadas
        query = ApplySpecificFilters(query, filters);

        // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier
        // profundidad de página. El orden queda forzado a (CreatedAt DESC, Id DESC)
        // para que el cursor sea estable; se ignora el SortBy recibido.
        var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
        if (usingKeyset)
        {
            var anchorCreatedAt = cursorCreatedAt!.Value;
            query = query
                .Where(ap => ap.CreatedAt < anchorCreatedAt ||
                             (ap.CreatedAt == anchorCreatedAt && ap.Id < cursorId!.Value))
                .OrderByDescending(ap => ap.CreatedAt)
                .ThenByDescending(ap => ap.Id);
        }
        else
        {
            // Apply sorting
            query = (sortBy?.ToLower() ?? "defaultSortField") switch
            {
                "createdat" => isDescending ? query.OrderByDescending(ap => ap.CreatedAt) : query.OrderBy(ap => ap.CreatedAt),
                "startdate" => isDescending ? query.OrderByDescending(ap => ap.PracticeStartDate) : query.OrderBy(ap => ap.PracticeStartDate),
                "institution" => isDescending ? query.OrderByDescending(ap => ap.InstitutionName) : query.OrderBy(ap => ap.InstitutionName),
                _ => query.OrderByDescending(ap => ap.CreatedAt)
            };
            // Desempate estable por Id (páginas deterministas con CreatedAt repetido)
            query = ((System.Linq.IOrderedQueryable<AcademicPractice>)query).ThenByDescending(ap => ap.Id);
        }

        // En keyset con SkipTotalCount se omite el COUNT (el frontend conserva el
        // total de la primera página): el COUNT con filtro escala igual de mal.
        var totalCount = usingKeyset && skipTotalCount ? -1 : await query.CountAsync(cancellationToken);
        // PageSize <= 0: sin paginación (entrega todo el resultado en una sola página)
        if (pageSize <= 0) { pageNumber = 1; pageSize = Math.Max(totalCount, 1); }
        // Keyset trae pageSize+1 filas para detectar HasMoreRows sin ejecutar COUNT.
        var pagedQuery = usingKeyset
            ? query.Take(pageSize + 1)
            : query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        var academicPractices = await pagedQuery.ToListAsync(cancellationToken);

        bool? hasMoreRows = null;
        if (usingKeyset)
        {
            hasMoreRows = academicPractices.Count > pageSize;
            if (hasMoreRows.Value)
                academicPractices = academicPractices.Take(pageSize).ToList();
        }

        var pagedPracticeIds = academicPractices.Select(ap => ap.Id).ToList();
        var teachersByInscription = await TeacherAssignmentLoader
            .LoadActiveByInscriptionIdsAsync(_context, pagedPracticeIds, cancellationToken);

        // Convert to detailed results (simplified for performance)
        var items = academicPractices.Select(ap => new AcademicPracticeWithDetails
        {
            AcademicPractice = ap,
            InscriptionModality = ap.InscriptionModality,
            StateStage = ap.StateStage,
            StageModality = ap.InscriptionModality.StageModality!,
            Modality = ap.InscriptionModality.Modality!,
            StateInscription = ap.InscriptionModality.StateInscription!,
            AcademicPeriod = ap.InscriptionModality.AcademicPeriod!,
            UserInscriptionModalities = new List<UserInscriptionModality>(),
            TeachingAssignments = new List<TeachingAssignment>(),
            Teachers = teachersByInscription.TryGetValue(ap.Id, out var teachers)
                ? teachers
                : new List<AssignedTeacher>(),
            Documents = new List<Document>()
        }).ToList();

        return new PaginatedResult<AcademicPracticeWithDetails>
        {
            Items = items,
            TotalRecords = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMoreRows = hasMoreRows
        };
    }

    public async Task<List<AcademicPracticeWithDetails>> GetAcademicPracticesByUserWithDetailsAsync(
        int userId, 
        bool? status = null, 
        CancellationToken cancellationToken = default)
    {
        // Get academic practices for a user through UserInscriptionModality
        var academicPracticeIds = await _context.Set<UserInscriptionModality>()
            .Where(uim => uim.IdUser == userId)
            .Select(uim => uim.IdInscriptionModality)
            .ToListAsync(cancellationToken);

        var query = _context.AcademicPractices
            .Where(ap => academicPracticeIds.Contains(ap.Id))
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.Modality)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.StateInscription)
            .Include(ap => ap.InscriptionModality)
                .ThenInclude(im => im.AcademicPeriod)
            .Include(ap => ap.StateStage)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(ap => ap.StatusRegister == status.Value);
        }

        var academicPractices = await query
            .OrderByDescending(ap => ap.CreatedAt)
            .ToListAsync(cancellationToken);

        var practiceIds = academicPractices.Select(ap => ap.Id).ToList();
        var teachersByInscription = await TeacherAssignmentLoader
            .LoadActiveByInscriptionIdsAsync(_context, practiceIds, cancellationToken);

        // Convert to detailed results
        var items = academicPractices.Select(ap => new AcademicPracticeWithDetails
        {
            AcademicPractice = ap,
            InscriptionModality = ap.InscriptionModality,
            StateStage = ap.StateStage,
            StageModality = ap.InscriptionModality.StageModality!,
            Modality = ap.InscriptionModality.Modality!,
            StateInscription = ap.InscriptionModality.StateInscription!,
            AcademicPeriod = ap.InscriptionModality.AcademicPeriod!,
            UserInscriptionModalities = new List<UserInscriptionModality>(),
            TeachingAssignments = new List<TeachingAssignment>(),
            Teachers = teachersByInscription.TryGetValue(ap.Id, out var teachers)
                ? teachers
                : new List<AssignedTeacher>(),
            Documents = new List<Document>()
        }).ToList();

        return items;
    }

    /// <summary>
    /// Asigna el nuevo estado (IdStateStage) a la práctica académica y, opcionalmente,
    /// sus observaciones generales y del evaluador; luego persiste los cambios.
    /// El cambio de IdStateStage encola AcademicPracticeStateChangedEvent, que se despacha
    /// en el CommitAsync del UnitOfWork para el avance de fase, permisos y estampado de fechas.
    /// </summary>
    public async Task<bool> UpdateAcademicPracticeStateAsync(
        int id, 
        int newStateStageId, 
        string? observations = null, 
        string? evaluatorObservations = null, 
        CancellationToken cancellationToken = default)
    {
        var academicPractice = await _context.AcademicPractices
            .FirstOrDefaultAsync(ap => ap.Id == id, cancellationToken);

        if (academicPractice == null) return false;

        academicPractice.IdStateStage = newStateStageId;

        if (!string.IsNullOrEmpty(observations))
        {
            academicPractice.Observations = observations;
        }

        if (!string.IsNullOrEmpty(evaluatorObservations))
        {
            academicPractice.EvaluatorObservations = evaluatorObservations;
        }

        academicPractice.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AcademicPracticePhaseProgress> GetPhaseProgressAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var academicPractice = await _context.AcademicPractices
            .Include(ap => ap.StateStage)
            .FirstOrDefaultAsync(ap => ap.Id == id, cancellationToken);

        if (academicPractice == null)
        {
            return new AcademicPracticePhaseProgress();
        }

        return new AcademicPracticePhaseProgress
        {
            CurrentPhase = academicPractice.StateStage.Code ?? "Unknown",
            CurrentState = academicPractice.StateStage.Description ?? "Unknown",
            Phase1Completed = academicPractice.AvalApprovalDate.HasValue,
            Phase2Completed = academicPractice.PlanApprovalDate.HasValue,
            Phase3Completed = academicPractice.FinalApprovalDate.HasValue,
            Phase1CompletionDate = academicPractice.AvalApprovalDate,
            Phase2CompletionDate = academicPractice.PlanApprovalDate,
            Phase3CompletionDate = academicPractice.FinalApprovalDate,
            PhaseDetails = new List<PhaseDetail>
            {
                new PhaseDetail
                {
                    PhaseNumber = 1,
                    PhaseName = "Aval",
                    PhaseCode = "aval",
                    IsCompleted = academicPractice.AvalApprovalDate.HasValue,
                    CompletionDate = academicPractice.AvalApprovalDate,
                    CurrentState = academicPractice.AvalApprovalDate.HasValue ? "Completed" : "Pending"
                },
                new PhaseDetail
                {
                    PhaseNumber = 2,
                    PhaseName = "Plan",
                    PhaseCode = "plan",
                    IsCompleted = academicPractice.PlanApprovalDate.HasValue,
                    CompletionDate = academicPractice.PlanApprovalDate,
                    CurrentState = academicPractice.PlanApprovalDate.HasValue ? "Completed" : "Pending"
                },
                new PhaseDetail
                {
                    PhaseNumber = 3,
                    PhaseName = "Final",
                    PhaseCode = "final",
                    IsCompleted = academicPractice.FinalApprovalDate.HasValue,
                    CompletionDate = academicPractice.FinalApprovalDate,
                    CurrentState = academicPractice.FinalApprovalDate.HasValue ? "Completed" : "Pending"
                }
            }
        };
    }

    /// <summary>
    /// Aplica filtros específicos para entidades relacionadas que no están en la entidad principal
    /// </summary>
    private static IQueryable<AcademicPractice> ApplySpecificFilters(IQueryable<AcademicPractice> query, Dictionary<string, string> filters)
    {
        foreach (var filter in filters)
        {
            var key = filter.Key.ToLower();
            var value = filter.Value;

            if (string.IsNullOrEmpty(value)) continue;

            switch (key)
            {
                case "idstagemodality":
                case "idstagemodality@eq":
                    if (int.TryParse(value, out int stageModalityId))
                    {
                        query = query.Where(ap => ap.InscriptionModality != null && 
                                                ap.InscriptionModality.IdStageModality == stageModalityId);
                    }
                    break;

                case "idmodality":
                case "idmodality@eq":
                    if (int.TryParse(value, out int modalityId))
                    {
                        query = query.Where(ap => ap.InscriptionModality != null && 
                                                ap.InscriptionModality.IdModality == modalityId);
                    }
                    break;

                case "idstateinscription":
                case "idstateinscription@eq":
                    if (int.TryParse(value, out int stateInscriptionId))
                    {
                        query = query.Where(ap => ap.InscriptionModality != null && 
                                                ap.InscriptionModality.IdStateInscription == stateInscriptionId);
                    }
                    break;

                case "idacademicperiod":
                case "idacademicperiod@eq":
                    if (int.TryParse(value, out int academicPeriodId))
                    {
                        query = query.Where(ap => ap.InscriptionModality != null && 
                                                ap.InscriptionModality.IdAcademicPeriod == academicPeriodId);
                    }
                    break;

                case "idstatestage":
                case "idstatestage@eq":
                    if (int.TryParse(value, out int stateStageId))
                    {
                        query = query.Where(ap => ap.IdStateStage == stateStageId);
                    }
                    break;

                // Mantener algunos filtros por nombre para casos específicos si es necesario
                case "modalityname@like":
                    query = query.Where(ap => ap.InscriptionModality != null && 
                                            ap.InscriptionModality.Modality != null &&
                                            ap.InscriptionModality.Modality.Name != null &&
                                            EF.Functions.ILike(EF.Functions.Unaccent(ap.InscriptionModality.Modality.Name), TeacherAssignmentLoader.LikePattern(value)));
                    break;

                case "stateinscriptionname@like":
                    query = query.Where(ap => ap.InscriptionModality != null && 
                                            ap.InscriptionModality.StateInscription != null &&
                                            ap.InscriptionModality.StateInscription.Name != null &&
                                            EF.Functions.ILike(EF.Functions.Unaccent(ap.InscriptionModality.StateInscription.Name), TeacherAssignmentLoader.LikePattern(value)));
                    break;

                // Agregar más filtros específicos según necesidad
            }
        }

        return query;
    }
}

