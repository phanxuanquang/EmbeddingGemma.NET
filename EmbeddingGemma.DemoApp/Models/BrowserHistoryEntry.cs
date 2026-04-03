namespace EmbeddingGemma.DemoApp.Models
{
    public sealed record BrowserHistoryEntry
    {
        /// <summary>
        /// The title of the webpage visited.
        /// </summary>
        public required string Title { get; init; } = default!;

        /// <summary>
        /// The URL of the webpage visited. 
        /// </summary>
        public required string Url { get; init; } = default!;

        /// <summary>
        /// Gets the date and time when the user last visited.
        /// </summary>
        public required DateTime LastVisitTime { get; init; }
    }

    public enum BrowserType : byte
    {
        Chrome,
        Edge,
        Firefox
    }
}
