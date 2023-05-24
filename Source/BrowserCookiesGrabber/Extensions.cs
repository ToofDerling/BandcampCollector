namespace BrowserCookiesGrabber
{
    public static class Extensions
    {
        public static DateTime UnixTimeToDateTime(this long unixTime)
        {
            return DateTime.UnixEpoch.AddSeconds(unixTime);
        }
        public static long DateTimeToUnixTime(this DateTime dateTime)
        {
            return (long)(dateTime - DateTime.UnixEpoch).TotalSeconds;
        }

        private static readonly DateTime ExpiresUtcEpoch = new(1601, 1, 1);

        public static DateTime ExpiresUtcEpochToDateTime(this long microSeconds)
        {
            return ExpiresUtcEpoch.AddMilliseconds(microSeconds / 1000);
        }
    }
}
