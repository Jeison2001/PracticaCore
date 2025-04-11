namespace Application.Shared.DTOs
{
    public class ExampleDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }
    }
}
