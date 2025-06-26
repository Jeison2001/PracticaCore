
namespace Application.Shared.DTOs.TeacherResearchProfile
{
    public class TeacherResearchProfileDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdResearchLine { get; set; }
        public int? IdResearchSubLine { get; set; }
        public string? ProfileDescription { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
