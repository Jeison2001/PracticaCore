using Domain.Common.Teachers;
using Domain.Entities;

namespace Domain.Common.Proposals
{
    /// <summary>
    /// Clase de transporte para encapsular una Propuesta junto con sus UserInscriptionModalities relacionados
    /// </summary>
    public class ProposalWithDetails
    {
        public required Proposal Proposal { get; set; }
        public List<UserInscriptionModality> UserInscriptionModalities { get; set; } = new List<UserInscriptionModality>();
        /// <summary>Docentes asignados activos (director/jurados) de la inscripción.</summary>
        public List<AssignedTeacher> Teachers { get; set; } = new List<AssignedTeacher>();
    }
}