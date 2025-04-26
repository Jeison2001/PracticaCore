using System;

namespace Domain.Entities
{
    public class RegisterModality : BaseEntity<int>
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Propiedades de navegación
        public virtual Modality? Modality { get; set; }
        public virtual StateInscription? StateInscription { get; set; }
        public virtual AcademicPeriod? AcademicPeriod { get; set; }
    }
}