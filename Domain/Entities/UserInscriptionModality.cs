namespace Domain.Entities
{
    public class UserInscriptionModality : BaseEntity<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; } 
        public new int? IdUserCreatedAt { get; set; }

        // Propiedades de navegación
        public virtual InscriptionModality? InscriptionModality { get; set; }
        public virtual User? User { get; set; }
    }
}