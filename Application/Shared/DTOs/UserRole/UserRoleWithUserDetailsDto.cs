using Application.Shared.DTOs;
using Application.Shared.DTOs.TeacherResearchProfile;
using Application.Shared.DTOs.TeachingAssignment;

namespace Application.Shared.DTOs.UserRole
{
    public class UserRoleWithUserDetailsDto : BaseDto<int>
    {
        public int IdUser { get; set; }
        public int IdRole { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        
        // Datos básicos del usuario
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Identification { get; set; } = string.Empty;

        // NUEVO: Solo para roleCode == TEACHER
        public List<TeachingAssignmentCargoInfoDto>? TeachingAssignmentsByCargo { get; set; }
        public List<TeacherResearchProfileDto>? TeacherResearchProfiles { get; set; }
    }
}