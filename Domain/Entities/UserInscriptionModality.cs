namespace Domain.Entities
{
    public class UserInscriptionModality : BaseEntity<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; } 

        // Propiedades de navegacion
        public virtual InscriptionModality? InscriptionModality { get; set; }
        public virtual User? User { get; set; }
    }
}