using Application.Shared.DTOs.UserInscriptionModalities;

using Application.Shared.DTOs.Teachers;

namespace Application.Shared.DTOs.Proposals
{
    public record ProposalWithDetailsResponseDto
    {
        public required ProposalDto Proposal { get; set; }
        public required string StateStageName { get; set; }
        public required string ResearchLineName { get; set; }
        public required string ResearchSubLineName { get; set; }
        public required List<UserInscriptionModalityDto> Students { get; set; }
        public string? StateStageCode { get; set; } // Código del estado de la fase
        /// <summary>Docentes asignados activos (director/jurados) — expuestos para mostrar y filtrar.</summary>
        public List<AssignedTeacherDto> Teachers { get; set; } = new();
    }
}
