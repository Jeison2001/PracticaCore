using Application.Shared.DTOs;

namespace Application.Shared.DTOs.TypeTeachingAssignment
{    public class TypeTeachingAssignmentDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxAssignments { get; set; }
        public new int? IdUserCreatedAt { get; set; }

    }
}