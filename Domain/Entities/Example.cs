namespace Domain.Entities
{
    public class Example : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }
    }
}
