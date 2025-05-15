namespace Domain.Entities
{
    /// <summary>
    /// Clase de transporte para encapsular una asignación docente junto con sus entidades relacionadas
    /// </summary>
    public class TeachingAssignmentWithDetails
    {
        public required TeachingAssignment TeachingAssignment { get; set; }
        public User? Teacher { get; set; }
        public TypeTeachingAssignment? TypeTeachingAssignment { get; set; }
    }
}
