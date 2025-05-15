namespace Application.Shared.DTOs.TeachingAssignment
{
    public class TeachingAssignmentTeacherDto
    {
        public int Id { get; set; } // Id del registro TeachingAssignment
        public int IdUser { get; set; } // Id del docente
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IdTypeTeachingAssignment { get; set; } // Id del tipo de asignación
        public string AssignmentType { get; set; } = string.Empty;
        public bool StatusRegister { get; set; } // Estado del registro (activo/inactivo)
    }
}