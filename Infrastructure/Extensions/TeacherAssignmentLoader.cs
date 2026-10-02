using Domain.Common.Teachers;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Utilidades compartidas para exponer y filtrar por docentes asignados
    /// (TeachingAssignment) en los listados de propuesta/anteproyecto/proyecto.
    /// </summary>
    public static class TeacherAssignmentLoader
    {
        /// <summary>Carga por lote los docentes asignados ACTIVOS de las inscripciones dadas.</summary>
        public static async Task<Dictionary<int, List<AssignedTeacher>>> LoadActiveByInscriptionIdsAsync(
            AppDbContext context,
            List<int> inscriptionIds,
            CancellationToken cancellationToken = default)
        {
            if (inscriptionIds.Count == 0)
                return new Dictionary<int, List<AssignedTeacher>>();

            var rows = await context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Include(ta => ta.Teacher)
                .Include(ta => ta.TypeTeachingAssignment)
                .Where(ta => inscriptionIds.Contains(ta.IdInscriptionModality)
                             && ta.StatusRegister
                             && ta.RevocationDate == null)
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.IdInscriptionModality)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => new AssignedTeacher
                    {
                        Id = r.Id,
                        IdTeacher = r.IdTeacher,
                        FullName = r.Teacher != null
                            ? $"{r.Teacher.FirstName} {r.Teacher.LastName}".Trim()
                            : string.Empty,
                        Email = r.Teacher?.Email ?? string.Empty,
                        CargoId = r.IdTypeTeachingAssignment,
                        CargoCode = r.TypeTeachingAssignment?.Code,
                        CargoName = r.TypeTeachingAssignment?.Name,
                        RevocationDate = r.RevocationDate,
                    }).ToList());
        }

        /// <summary>
        /// Separa las claves de filtro por docente y cargo (TeacherName@like, TeacherEmail@like,
        /// TeacherId@eq, TeacherCargoId@eq, TeacherCargoCode@eq, TeacherCargoName@eq) del resto
        /// de filtros, que siguen su curso normal (FilterBuilder).
        /// </summary>
        public static TeacherFilterSet ExtractTeacherFilters(Dictionary<string, string>? filters)
        {
            var set = new TeacherFilterSet();

            foreach (var f in filters ?? new Dictionary<string, string>())
            {
                var prop = f.Key.Contains('@') ? f.Key.Split('@', 2)[0] : f.Key;
                switch (prop.ToLowerInvariant())
                {
                    case "teachername": set.Name = f.Value; break;
                    case "teacheremail": set.Email = f.Value; break;
                    case "teacherid":
                        if (int.TryParse(f.Value, out var tid)) set.Id = tid;
                        break;
                    case "teachercargoid":
                        if (int.TryParse(f.Value, out var cid)) set.CargoId = cid;
                        break;
                    case "teachercargocode": set.CargoCode = f.Value; break;
                    case "teachercargoname": set.CargoName = f.Value; break;
                    default: set.Remaining[f.Key] = f.Value; break;
                }
            }

            return set;
        }

        /// <summary>Filtros de docente/cargo extraídos de la petición (Remaining = el resto).</summary>
        public class TeacherFilterSet
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public int? Id { get; set; }
            public int? CargoId { get; set; }
            public string? CargoCode { get; set; }
            public string? CargoName { get; set; }
            public Dictionary<string, string> Remaining { get; } = new();

            /// <summary>¿Hay algún criterio de docente/cargo activo?</summary>
            public bool HasAny => Name != null || Email != null || Id != null
                                  || CargoId != null || CargoCode != null || CargoName != null;
        }

        /// <summary>Patrón ILIKE con escapes básicos de comodines.</summary>
        public static string LikePattern(string value) =>
            "%" + value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }
}
