namespace Application.Shared.DTOs.AcademicPrograms
{
    public record AcademicProgramDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int IdFaculty { get; set; }
    }
}
