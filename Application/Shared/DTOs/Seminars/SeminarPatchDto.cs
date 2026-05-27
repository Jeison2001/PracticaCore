namespace Application.Shared.DTOs.Seminars
{
    public record SeminarPatchDto
    {
        public int? IdStateStage { get; set; }
        public string? Observations { get; set; }
    }
}
