namespace Application.Shared.DTOs.UserRoles
{
    public record UserRoleDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }

        /// <summary>
        /// Opcional: cargos habilitados (ids de TypeTeachingAssignment) a asignar al docente
        /// cuando el rol asignado es TEACHER. Solo se procesan en ese caso.
        /// </summary>
        public List<int>? TypeTeachingAssignmentIds { get; set; }
    }
}
