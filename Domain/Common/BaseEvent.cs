using MediatR;

namespace Domain.Common
{
    public interface IDomainEvent : INotification
    {
    }

    public abstract record BaseEvent : IDomainEvent
    {
        public DateTimeOffset DateOccurred { get; set; } = DateTimeOffset.UtcNow;
    }
}
