// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Domain\Entities\StateInscription.cs
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class StateInscription : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSelectable { get; set; } = true;
        public new int? IdUserCreatedAt { get; set; }
    }
}