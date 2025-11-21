namespace Application.Shared.DTOs.TypeTeachingAssignments
{    public record TypeTeachingAssignmentDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxAssignments { get; set; }
        public new int? IdUserCreatedAt { get; set; }

    }
}
