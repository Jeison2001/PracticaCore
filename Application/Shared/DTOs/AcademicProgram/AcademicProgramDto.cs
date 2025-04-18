using Application.Shared.DTOs.Faculty;

namespace Application.Shared.DTOs.AcademicProgram
{
    public class AcademicProgramDto : BaseDto<int>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int IdFaculty { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}