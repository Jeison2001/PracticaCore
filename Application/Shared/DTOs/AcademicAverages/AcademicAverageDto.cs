namespace Application.Shared.DTOs.AcademicAverages
{
    public record AcademicAverageDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public decimal? CertifiedAverage { get; set; }
        public bool? HasFailedSubjects { get; set; }
        public string? Observations { get; set; }
    }
}
