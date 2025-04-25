namespace Application.Shared.DTOs.Faculty
{
    public class FacultyDto : BaseDto<int>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}