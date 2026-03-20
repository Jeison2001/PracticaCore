using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;

namespace Domain.Entities
{
    public abstract class BaseEntity<TId> where TId : struct
    {
        public TId Id { get; set; }
        public int? IdUserCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? IdUserUpdatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
        public bool StatusRegister { get; set; } = true;

        private readonly List<IDomainEvent> _domainEvents = new();

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
