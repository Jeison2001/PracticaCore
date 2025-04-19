using System;

namespace Domain.Entities
{
    public class RegisterModalityStudent : BaseEntity<long>
    {
        public long IdRegisterModality { get; set; }
        public int IdUser { get; set; } 
        public new int? IdUserCreatedAt { get; set; }

        // Propiedades de navegación
        public virtual RegisterModality? RegisterModality { get; set; }
        public virtual User? User { get; set; }
    }
}