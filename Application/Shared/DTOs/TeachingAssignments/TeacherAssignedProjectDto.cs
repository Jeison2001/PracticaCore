namespace Application.Shared.DTOs.TeachingAssignments
{
    public record TeacherAssignedProjectDto
    {
        public int Id { get; init; }
        public int IdInscriptionModality { get; init; }
        public int IdModality { get; init; }
        public string ModalityCode { get; init; } = string.Empty;
        public string ModalityName { get; init; } = string.Empty;
        public int IdStateInscription { get; init; }
        public string StateInscriptionCode { get; init; } = string.Empty;
        public string StateInscriptionName { get; init; } = string.Empty;
        public int IdTypeTeachingAssignment { get; init; }
        public string CargoCode { get; init; } = string.Empty;
        public string CargoName { get; init; } = string.Empty;
        public bool StatusRegister { get; init; }
        public DateTimeOffset? RevocationDate { get; init; }
        public DateTimeOffset AssignedAt { get; init; }
    }
}
