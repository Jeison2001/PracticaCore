using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Detalles relacionados (estudiantes, asignaciones docentes y documentos) cargados
    /// POR LOTE para una página completa, eliminando el patrón N+1 de PopulateDetails
    /// (antes: 3 consultas por ítem; ahora: 3 consultas por página).
    /// </summary>
    public class ModalityDetailsBatch
    {
        public required Dictionary<int, List<UserInscriptionModality>> Users { get; init; }
        public required Dictionary<int, List<TeachingAssignment>> Assignments { get; init; }
        public required Dictionary<int, List<Document>> Documents { get; init; }

        public List<UserInscriptionModality> GetUsers(int inscriptionId) =>
            Users.TryGetValue(inscriptionId, out var v) ? v : new List<UserInscriptionModality>();

        public List<TeachingAssignment> GetAssignments(int inscriptionId) =>
            Assignments.TryGetValue(inscriptionId, out var v) ? v : new List<TeachingAssignment>();

        public List<Document> GetDocuments(int inscriptionId) =>
            Documents.TryGetValue(inscriptionId, out var v) ? v : new List<Document>();

        public static async Task<ModalityDetailsBatch> LoadAsync(
            AppDbContext context,
            List<int> inscriptionIds,
            CancellationToken cancellationToken = default)
        {
            if (inscriptionIds.Count == 0)
            {
                return new ModalityDetailsBatch
                {
                    Users = new Dictionary<int, List<UserInscriptionModality>>(),
                    Assignments = new Dictionary<int, List<TeachingAssignment>>(),
                    Documents = new Dictionary<int, List<Document>>(),
                };
            }

            var users = await context.Set<UserInscriptionModality>()
                .AsNoTracking()
                .Where(uim => inscriptionIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .ToListAsync(cancellationToken);

            var assignments = await context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Where(ta => inscriptionIds.Contains(ta.IdInscriptionModality))
                .Include(ta => ta.Teacher)
                .Include(ta => ta.TypeTeachingAssignment)
                .ToListAsync(cancellationToken);

            var documents = await context.Set<Document>()
                .AsNoTracking()
                .Where(d => d.IdInscriptionModality.HasValue && inscriptionIds.Contains(d.IdInscriptionModality.Value))
                .Include(d => d.DocumentType)
                .ToListAsync(cancellationToken);

            return new ModalityDetailsBatch
            {
                Users = users.GroupBy(u => u.IdInscriptionModality).ToDictionary(g => g.Key, g => g.ToList()),
                Assignments = assignments.GroupBy(a => a.IdInscriptionModality).ToDictionary(g => g.Key, g => g.ToList()),
                Documents = documents.GroupBy(d => d.IdInscriptionModality!.Value).ToDictionary(g => g.Key, g => g.ToList()),
            };
        }
    }
}
