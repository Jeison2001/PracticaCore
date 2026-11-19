namespace Domain.Entities
{
    public class UserInscriptionModality : BaseEntity<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; }
        public bool IsConfirmed { get; set; } = true;

        // Navegacion — solo la coleccion esta en InscriptionModality
        public virtual User? User { get; set; }
    }
}