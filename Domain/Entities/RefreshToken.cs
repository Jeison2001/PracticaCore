namespace Domain.Entities
{
    public class RefreshToken : BaseEntity<long>
    {
        public int IdUser { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt == null && !IsExpired;

        public virtual User User { get; set; } = null!;
    }
}
