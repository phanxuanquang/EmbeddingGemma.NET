namespace EmbeddingGemma.DemoApp.Models
{
    public class WindowsEventLog
    {
        public string Source { get; set; } = default!;
        public string LogLevel { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string LogMessage { get; set; } = default!;
    }
}
