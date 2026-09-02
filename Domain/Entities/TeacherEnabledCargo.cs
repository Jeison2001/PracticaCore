namespace Domain.Entities
{
    /// <summary>
    /// Relación N:M entre docente (User) y cargo habilitado (TypeTeachingAssignment).
    /// Define qué cargos puede ejercer un docente en asignaciones docentes.
    /// </summary>
    public class TeacherEnabledCargo : BaseEntity<int>
    {
        public int IdUser { get; set; }
        public int IdTypeTeachingAssignment { get; set; }

        // Relaciones
        public virtual User User { get; set; } = null!;
        public virtual TypeTeachingAssignment TypeTeachingAssignment { get; set; } = null!;
    }
}
