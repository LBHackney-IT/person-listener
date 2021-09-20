using System;

namespace PersonListener.Infrastructure
{
    // TODO - Should probably go in a common NuGet package

    public static class DateTimeExtensions
    {
        private static readonly string _dateFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ";

        public static string ToFormattedDateTime(this DateTime dt)
        {
            return dt.ToString(_dateFormat);
        }
    }
}
