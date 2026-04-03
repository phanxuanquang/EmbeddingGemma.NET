using EmbeddingGemma.DemoApp.Models;
using System.Diagnostics.Eventing.Reader;

namespace EmbeddingGemma.DemoApp.Helpers
{
    public static class WindowsEventLogHelper
    {
        public static IEnumerable<WindowsEventLog> ReadApplicationLogs(int maxRecords = 500, DateTime? from = null)
        {
            var queryString = BuildQuery(from);

            var query = new EventLogQuery("Application", PathType.LogName, queryString)
            {
                ReverseDirection = true
            };

            using var reader = new EventLogReader(query);

            int count = 0;
            for (EventRecord? record = reader.ReadEvent(); record != null && count < maxRecords; record = reader.ReadEvent())
            {
                yield return new WindowsEventLog
                {
                    Source = record.ProviderName ?? "Unknown",
                    LogLevel = MapLevel(record.Level),
                    Timestamp = record.TimeCreated ?? DateTime.MinValue,
                    LogMessage = SafeFormatMessage(record)
                };

                count++;
            }
        }

        private static string BuildQuery(DateTime? from)
        {
            if (!from.HasValue)
                return "*";

            var utc = from.Value.ToUniversalTime().ToString("o");
            return $@"
            *[System[TimeCreated[@SystemTime >= '{utc}']]]
        ";
        }

        private static string MapLevel(byte? level)
        {
            return level switch
            {
                1 => "Critical",
                2 => "Error",
                3 => "Warning",
                4 => "Information",
                5 => "Verbose",
                _ => "Unknown"
            };
        }

        private static string SafeFormatMessage(EventRecord record)
        {
            try
            {
                var fullMessage = record.FormatDescription() ?? "[No message]";
                return fullMessage.Split([Environment.NewLine], StringSplitOptions.None)[0];
            }
            catch
            {
                return "[Unable to format message]";
            }
        }
    }
}
