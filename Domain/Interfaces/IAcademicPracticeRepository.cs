using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces
{
    public interface IAcademicPracticeRepository : IRepository<AcademicPractice, int>, IScopedService
    {
        /// <summary>
        /// Obtiene prácticas académicas asignadas a un profesor a través de la entidad TeachingAssignment con paginación
        /// </summary>
        /// <param name="teacherId">ID del profesor</param>
        /// <param name="pageNumber">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="sortBy">Campo por el cual ordenar</param>
        /// <param name="isDescending">Indica si el ordenamiento es descendente</param>
        /// <param name="filters">Filtros adicionales que pueden incluir el estado</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado paginado de prácticas académicas con todos sus detalles relacionados</returns>
        Task<PaginatedResult<AcademicPracticeWithDetails>> GetAcademicPracticesByTeacherWithDetailsPaginatedAsync(
            int teacherId,
            int pageNumber, 
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene prácticas académicas con sus detalles para un usuario, evitando operaciones paralelas en el DbContext
        /// </summary>
        Task<List<AcademicPracticeWithDetails>> GetAcademicPracticesByUserWithDetailsAsync(
            int userId,
            bool? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todas las prácticas académicas con paginación y detalles completos
        /// </summary>
        Task<PaginatedResult<AcademicPracticeWithDetails>> GetAllAcademicPracticesWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy,
            bool isDescending, 
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una práctica académica con todos sus detalles
        /// </summary>
        Task<AcademicPracticeWithDetails?> GetAcademicPracticeWithDetailsAsync(
            int id, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza el estado de una práctica académica y maneja las fechas de aprobación automáticamente
        /// </summary>
        Task<bool> UpdateAcademicPracticeStateAsync(
            int id, 
            int newStateStageId, 
            string? observations = null,
            string? evaluatorObservations = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el progreso de fases de una práctica académica
        /// </summary>
        Task<AcademicPracticePhaseProgress> GetPhaseProgressAsync(
            int id, 
            CancellationToken cancellationToken = default);
    }

    // Clases auxiliares para los métodos del repositorio
    public class AcademicPracticeWithDetails
    {
        public AcademicPractice AcademicPractice { get; set; } = null!;
        public InscriptionModality? InscriptionModality { get; set; }
        public StateStage? StateStage { get; set; }
        public StageModality? StageModality { get; set; }
        public Modality? Modality { get; set; }
        public StateInscription? StateInscription { get; set; }
        public AcademicPeriod? AcademicPeriod { get; set; }
        public List<UserInscriptionModality> UserInscriptionModalities { get; set; } = new();
        public List<TeachingAssignment> TeachingAssignments { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
    }

    public class AcademicPracticePhaseProgress
    {
        public string CurrentPhase { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public bool Phase1Completed { get; set; }
        public bool Phase2Completed { get; set; }
        public bool Phase3Completed { get; set; }
        public DateTime? Phase1CompletionDate { get; set; }
        public DateTime? Phase2CompletionDate { get; set; }
        public DateTime? Phase3CompletionDate { get; set; }
        public List<PhaseDetail> PhaseDetails { get; set; } = new();
    }

    public class PhaseDetail
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public string PhaseCode { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public List<StateDetail> States { get; set; } = new();
    }

    public class StateDetail
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
    }
}
