namespace Application.Shared.DTOs.StateProjectFinal
{
    public class StateProjectFinalDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsFinalState { get; set; }
    }
}