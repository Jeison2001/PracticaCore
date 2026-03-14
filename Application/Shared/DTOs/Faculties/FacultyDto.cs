namespace Application.Shared.DTOs.Faculties
{
    public record FacultyDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
