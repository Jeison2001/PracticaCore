namespace Domain.Entities
{
    public class User : BaseEntity<int>
    {
        public int IdIdentificationType { get; set; }
        public string Identification { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int IdAcademicProgram { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CurrentAcademicPeriod { get; set; }
        public double? CumulativeAverage { get; set; }
        public int? ApprovedCredits { get; set; }
        public int? TotalAcademicCredits { get; set; }
        public string? Observation { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Relaciones
        public virtual IdentificationType IdentificationType { get; set; }
        public virtual AcademicProgram AcademicProgram { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();

    }
}