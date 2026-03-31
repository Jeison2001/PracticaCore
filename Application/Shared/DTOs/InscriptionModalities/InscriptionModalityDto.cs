namespace Application.Shared.DTOs.InscriptionModalities
{
    public record InscriptionModalityDto : BaseDto<int>
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public int? IdStageModality { get; set; }
        public DateTimeOffset? ApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}

