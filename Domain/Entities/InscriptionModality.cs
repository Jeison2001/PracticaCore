using System;

namespace Domain.Entities
{
    public class InscriptionModality : BaseEntity<int>
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public int? IdStateWorkGrade { get; set; } // Nuevo campo
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Propiedades de navegación
        public virtual Modality? Modality { get; set; }
        public virtual StateInscription? StateInscription { get; set; }
        public virtual AcademicPeriod? AcademicPeriod { get; set; }
        public virtual Proposal? Proposal { get; set; }
        public virtual StateWorkGrade? StateWorkGrade { get; set; } // Nueva navegación
        public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    }
}