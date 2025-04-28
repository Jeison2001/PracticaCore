namespace Application.Shared.DTOs
{
    public class UpdateStatusRequestDto
    {
        public bool StatusRegister { get; set; }
        public int IdUserUpdateAt { get; set; }
        public string OperationRegister { get; set; }
    }
}