namespace Application.Shared.DTOs
{
    public record BaseDto<TId> where TId : struct
    {
        public TId Id { get; set; }
        public int? IdUserCreatedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int? IdUserUpdatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
        public bool StatusRegister { get; set; } = true;
    }
}

