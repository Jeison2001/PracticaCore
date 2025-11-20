namespace Application.Shared.DTOs.AcademicProgram
{
    public class AcademicProgramDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int IdFaculty { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}