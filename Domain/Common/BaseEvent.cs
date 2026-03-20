using MediatR;

namespace Domain.Common
{
    public interface IDomainEvent : INotification
    {
    }

    public abstract record BaseEvent : IDomainEvent
    {
        public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
    }
}
