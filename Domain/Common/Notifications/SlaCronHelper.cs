namespace Domain.Common.Notifications
{
    public static class SlaCronHelper
    {
        public static string BuildCron(int hour, int minute, bool weekdaysOnly) =>
            $"{minute} {hour} * * {(weekdaysOnly ? "1-5" : "*")}";
    }
}
