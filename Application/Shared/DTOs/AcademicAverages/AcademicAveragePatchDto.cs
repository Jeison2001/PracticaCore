namespace Application.Shared.DTOs.AcademicAverages
{
    public record AcademicAveragePatchDto
    {
        public int? IdStateStage { get; set; }
        public string? Observations { get; set; }
    }
}
