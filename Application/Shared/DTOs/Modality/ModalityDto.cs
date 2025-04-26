namespace Application.Shared.DTOs.Modality
{
    public class ModalityDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaximumTermPeriods { get; set; }
        public bool AllowsExtension { get; set; } = false;
        public bool RequiresDirector { get; set; } = true;
        public int MaxStudents { get; set; } = 1;
        public string? SpecificRequirements { get; set; }
        public bool RequiresResearchHotbed { get; set; } = false;
        public bool RequiresApproval { get; set; } = true;
        public new int? IdUserCreatedAt { get; set; }
    }
}