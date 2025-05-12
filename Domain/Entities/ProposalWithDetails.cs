using System.Collections.Generic;

namespace Domain.Entities
{
    /// <summary>
    /// Clase de transporte para encapsular una Propuesta junto con sus UserInscriptionModalities relacionados
    /// </summary>
    public class ProposalWithDetails
    {
        public required Proposal Proposal { get; set; }
        public List<UserInscriptionModality> UserInscriptionModalities { get; set; } = new List<UserInscriptionModality>();
    }
}