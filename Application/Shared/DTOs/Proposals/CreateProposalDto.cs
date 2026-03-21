namespace Application.Shared.DTOs.Proposals
{
    public record CreateProposalDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GeneralObjective { get; set; } = string.Empty;
        public List<string> SpecificObjectives { get; set; } = new List<string>();
        public int IdResearchLine { get; set; }
        public int IdResearchSubLine { get; set; }
        public string? Observation { get; set; }
    }
}
