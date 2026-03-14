
namespace Application.Shared.DTOs.TeacherResearchProfiles
{
    public record TeacherResearchProfileDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdResearchLine { get; set; }
        public int? IdResearchSubLine { get; set; }
        public string? ProfileDescription { get; set; }
    }
}
