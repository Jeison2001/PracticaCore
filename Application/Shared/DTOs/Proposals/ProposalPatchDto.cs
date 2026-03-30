namespace Application.Shared.DTOs.Proposals
{
    /// <summary>
    /// DTO para actualización parcial (PATCH) de Proposal.
    /// Solo incluye campos que el frontend puede modificar: estado y observación.
    /// </summary>
    public record ProposalPatchDto
    {
        public int? IdStateStage { get; set; }
        public string? Observation { get; set; }
    }
}
