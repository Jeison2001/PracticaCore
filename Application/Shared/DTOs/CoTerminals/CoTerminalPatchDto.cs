namespace Application.Shared.DTOs.CoTerminals
{
    public record CoTerminalPatchDto
    {
        public int? IdStateStage { get; set; }
        public string? Observations { get; set; }
    }
}
