using System;

namespace Application.Shared.DTOs.UserInscriptionModality
{
    public class UserInscriptionModalityDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}