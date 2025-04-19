using System;

namespace Domain.Entities
{
    public class RegisterModality : BaseEntity<long>
    {
        public long IdModality { get; set; }
        public long IdRegisterModalityState { get; set; }
        public long IdAcademicPeriod { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Propiedades de navegación
        public virtual Modality? Modality { get; set; }
        public virtual RegisterModalityState? RegisterModalityState { get; set; }
        public virtual AcademicPeriod? AcademicPeriod { get; set; }
    }
}