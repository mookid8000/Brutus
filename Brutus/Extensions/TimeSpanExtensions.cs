using System;

namespace Brutus.Extensions
{
    static  class TimeSpanExtensions
    {
        public static string ToHumanReadableTimeSpan(this TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.FromMinutes(1))
            {
                var (val, end) = ToValuePossiblyPlural(timeSpan.TotalSeconds);

                return $"{val} second{end}";
            }

            if (timeSpan < TimeSpan.FromHours(1))
            {
                var (val, end) = ToValuePossiblyPlural(timeSpan.TotalMinutes);

                return $"{val} minute{end}";
            }

            if (timeSpan < TimeSpan.FromDays(1))
            {
                var (val, end) = ToValuePossiblyPlural(timeSpan.TotalHours);

                return $"{val} hour{end}";
            }

            var value = ToValuePossiblyPlural(timeSpan.TotalDays);

            return $"{value.val} day{value.end}";
        }

        static (string val, string end) ToValuePossiblyPlural(double value)
        {
            var roundedValue = (int)Math.Round(value);
            var valueString = roundedValue.ToString();
            return roundedValue == 1 ? (valueString, "") : (valueString, "s");
        }
    }
}