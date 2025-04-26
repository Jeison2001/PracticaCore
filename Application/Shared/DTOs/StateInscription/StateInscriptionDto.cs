// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Application\Shared\DTOs\StateInscription\StateInscriptionDto.cs
using System;

namespace Application.Shared.DTOs.StateInscription
{
    public class StateInscriptionDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSelectable { get; set; } = true;
        public new int? IdUserCreatedAt { get; set; }
    }
}