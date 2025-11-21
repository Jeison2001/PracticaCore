namespace Application.Shared.DTOs
{
    public record UpdateStatusRequestDto
    {
        public bool StatusRegister { get; set; }
        public int IdUserUpdateAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
    }
}
