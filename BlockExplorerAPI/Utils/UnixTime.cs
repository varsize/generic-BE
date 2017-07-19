using System;
namespace BlockExplorerAPI.Utils
{
    public static class UnixTime
    {
        static DateTime unixEpoch;
        static UnixTime()
        {
            unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static long Now { get { return GetFromDateTime(DateTime.UtcNow); } }
        public static long GetFromDateTime(DateTime d) { return (long)(d - unixEpoch).TotalSeconds; }
        public static DateTime ConvertToDateTime(long unixtime) { return unixEpoch.AddSeconds(unixtime); }
    }
}