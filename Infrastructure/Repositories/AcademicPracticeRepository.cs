using Domain.Entities;
using Domain.Interfaces;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            Documents = documents
        };
    }

    public async Task<PaginatedResult<AcademicPracticeWithDetails>> GetAllAcademicPracticesWithDetailsPaginatedAsync(
        int pageNumber, 
        int pageSize, 
        string sortBy, 
        bool isDescending, 
        Dictionary<string, string> filters, 
        CancellationToken cancellationToken = default)
    {
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

        // Apply filters
        if (filters.ContainsKey("state") && !string.IsNullOrEmpty(filters["state"]))
        {
            query = query.Where(ap => ap.StateStage.Code == filters["state"]);
        }

        if (filters.ContainsKey("modality") && !string.IsNullOrEmpty(filters["modality"]))
        {
            query = query.Where(ap => ap.InscriptionModality.Modality!.Code == filters["modality"]);
        }

        if (filters.ContainsKey("institution") && !string.IsNullOrEmpty(filters["institution"]))
        {
            query = query.Where(ap => ap.InstitutionName != null && 
                                     ap.InstitutionName.Contains(filters["institution"]));
        }

        // Apply sorting
        query = (sortBy?.ToLower() ?? "defaultSortField") switch
        {
            "createdat" => isDescending ? query.OrderByDescending(ap => ap.CreatedAt) : query.OrderBy(ap => ap.CreatedAt),
            "startdate" => isDescending ? query.OrderByDescending(ap => ap.PracticeStartDate) : query.OrderBy(ap => ap.PracticeStartDate),
            "institution" => isDescending ? query.OrderByDescending(ap => ap.InstitutionName) : query.OrderBy(ap => ap.InstitutionName),
            _ => query.OrderByDescending(ap => ap.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var academicPractices = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Convert to detailed results (simplified for performance)
        var items = academicPractices.Select(ap => new AcademicPracticeWithDetails
        {
            AcademicPractice = ap,
            InscriptionModality = ap.InscriptionModality,
            StateStage = ap.StateStage,
            StageModality = ap.InscriptionModality?.StageModality,
            Modality = ap.InscriptionModality?.Modality,
            StateInscription = ap.InscriptionModality?.StateInscription,
            AcademicPeriod = ap.InscriptionModality?.AcademicPeriod,
            UserInscriptionModalities = new List<UserInscriptionModality>(),
            TeachingAssignments = new List<TeachingAssignment>(),
            Documents = new List<Document>()
        }).ToList();

        return new PaginatedResult<AcademicPracticeWithDetails>
        {
            Items = items,
            TotalRecords = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<AcademicPracticeWithDetails>> GetAcademicPracticesByTeacherWithDetailsPaginatedAsync(
        int teacherId, 
        int pageNumber, 
        int pageSize, 
        string sortBy, 
        bool isDescending, 
        Dictionary<string, string> filters, 
        CancellationToken cancellationToken = default)
    {
        // Get academic practices assigned to a teacher through teaching assignments
        var academicPracticeIds = await _context.Set<TeachingAssignment>()
            .Where(ta => ta.IdTeacher == teacherId)
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

        // Apply filters
        if (filters.ContainsKey("state") && !string.IsNullOrEmpty(filters["state"]))
        {
            query = query.Where(ap => ap.StateStage.Code == filters["state"]);
        }

        if (filters.ContainsKey("modality") && !string.IsNullOrEmpty(filters["modality"]))
        {
            query = query.Where(ap => ap.InscriptionModality.Modality!.Code == filters["modality"]);
        }

        // Apply sorting
        query = (sortBy?.ToLower() ?? "defaultSortField") switch
        {
            "createdat" => isDescending ? query.OrderByDescending(ap => ap.CreatedAt) : query.OrderBy(ap => ap.CreatedAt),
            "startdate" => isDescending ? query.OrderByDescending(ap => ap.PracticeStartDate) : query.OrderBy(ap => ap.PracticeStartDate),
            "institution" => isDescending ? query.OrderByDescending(ap => ap.InstitutionName) : query.OrderBy(ap => ap.InstitutionName),
            _ => query.OrderByDescending(ap => ap.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var academicPractices = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
            Documents = new List<Document>()
        }).ToList();

        return new PaginatedResult<AcademicPracticeWithDetails>
        {
            Items = items,
            TotalRecords = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
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
            Documents = new List<Document>()
        }).ToList();

        return items;
    }

    public async Task<bool> UpdateAcademicPracticeStateAsync(
        int id, 
        int newStateStageId, 
        string phaseType, 
        DateTime? approvalDate = null, 
        string? observations = null, 
        string? evaluatorObservations = null, 
        CancellationToken cancellationToken = default)
    {
        var academicPractice = await _context.AcademicPractices
            .FirstOrDefaultAsync(ap => ap.Id == id, cancellationToken);

        if (academicPractice == null) return false;

        academicPractice.IdStateStage = newStateStageId;
        
        // Update phase-specific approval dates based on phase type
        if (approvalDate.HasValue)
        {
            switch (phaseType.ToLower())
            {
                case "aval":
                case "approval":
                    academicPractice.AvalApprovalDate = approvalDate;
                    break;
                case "plan":
                    academicPractice.PlanApprovalDate = approvalDate;
                    break;
                case "development":
                    academicPractice.DevelopmentCompletionDate = approvalDate;
                    break;
                case "finalreport":
                    academicPractice.FinalReportApprovalDate = approvalDate;
                    break;
                case "final":
                    academicPractice.FinalApprovalDate = approvalDate;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(observations))
        {
            academicPractice.Observations = observations;
        }

        if (!string.IsNullOrEmpty(evaluatorObservations))
        {
            academicPractice.EvaluatorObservations = evaluatorObservations;
        }

        academicPractice.UpdatedAt = DateTime.UtcNow;

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
}
