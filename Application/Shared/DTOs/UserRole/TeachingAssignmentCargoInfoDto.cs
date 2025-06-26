using System.Collections.Generic;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.DTOs.TypeTeachingAssignment;

namespace Application.Shared.DTOs.UserRole
{
    /// <summary>
    /// DTO para agrupar asignaciones activas de un docente por cargo (tipo de asignación) y su límite.
    /// </summary>
    public class TeachingAssignmentCargoInfoDto
    {
        public int IdTypeTeachingAssignment { get; set; }
        public string CargoName { get; set; } = string.Empty;
        public int? MaxAssignments { get; set; }
        public List<TeachingAssignmentDto> ActiveAssignments { get; set; } = new();
    }
}
