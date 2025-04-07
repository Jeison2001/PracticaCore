namespace Application.Shared.DTOs
{
    public class BaseDto<TId> where TId : struct
    {
        public TId Id { get; set; }
        public int IdUserCreatedAt { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public int? IdUserUpdatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
        public bool StatusRegister { get; set; } = true;
    }
}
