namespace Domain.Common.Notifications
{
    public sealed record EmailEventDispatchResult(int EmailsSent, IReadOnlyList<string> Recipients);
}
