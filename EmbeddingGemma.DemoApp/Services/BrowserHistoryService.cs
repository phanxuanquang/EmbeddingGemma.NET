using EmbeddingGemma.DemoApp.Models;
using Microsoft.Data.Sqlite;

namespace EmbeddingGemma.DemoApp.Services
{
    public interface IBrowserHistoryService
    {
        Task<List<BrowserHistoryEntry>> GetBrowserHistoriesAsync(BrowserType browserType, DateTime from, DateTime? to);

        List<BrowserType> GetAvailableBrowserTypes();
    }

    public class BrowserHistoryService : IBrowserHistoryService
    {
        private static readonly DateTime ChromeEpoch = new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public async Task<List<BrowserHistoryEntry>> GetBrowserHistoriesAsync(BrowserType browserType, DateTime from, DateTime? to)
        {
            var endTime = (to ?? DateTime.Now).ToUniversalTime();
            var startTime = from.ToUniversalTime();

            return browserType switch
            {
                BrowserType.Chrome => await GetChromiumHistoryAsync(GetChromeDatabasePath(), startTime, endTime),
                BrowserType.Edge => await GetChromiumHistoryAsync(GetEdgeDatabasePath(), startTime, endTime),
                BrowserType.Firefox => await GetFirefoxHistoryAsync(GetFirefoxDatabasePath(), startTime, endTime),
                _ => throw new ArgumentOutOfRangeException(nameof(browserType), browserType, null)
            };
        }

        public List<BrowserType> GetAvailableBrowserTypes()
        {
            var availableBrowserTypes = new List<BrowserType>();

            if (File.Exists(GetChromeDatabasePath()))
                availableBrowserTypes.Add(BrowserType.Chrome);

            if (File.Exists(GetEdgeDatabasePath()))
                availableBrowserTypes.Add(BrowserType.Edge);

            try
            {
                if (File.Exists(GetFirefoxDatabasePath()))
                    availableBrowserTypes.Add(BrowserType.Firefox);
            }
            catch (DirectoryNotFoundException) { }

            return availableBrowserTypes;
        }

        private static string GetChromeDatabasePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "History");
        }

        private static string GetEdgeDatabasePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "History");
        }

        private static string GetFirefoxDatabasePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var profilesDir = Path.Combine(appData, "Mozilla", "Firefox", "Profiles");

            if (!Directory.Exists(profilesDir))
                throw new DirectoryNotFoundException($"Firefox profiles directory not found: {profilesDir}");

            var defaultProfile = Directory.EnumerateDirectories(profilesDir)
                .FirstOrDefault(d => Path.GetFileName(d).Contains(".default", StringComparison.OrdinalIgnoreCase))
                ?? Directory.EnumerateDirectories(profilesDir).FirstOrDefault()
                ?? throw new DirectoryNotFoundException("No Firefox profile found.");

            return Path.Combine(defaultProfile, "places.sqlite");
        }

        private static async Task<List<BrowserHistoryEntry>> GetChromiumHistoryAsync(string dbPath, DateTime from, DateTime to)
        {
            if (!File.Exists(dbPath))
                return [];

            var tempPath = Path.GetTempFileName();
            try
            {
                File.Copy(dbPath, tempPath, overwrite: true);

                long fromTicks = (long)(from - ChromeEpoch).TotalMicroseconds;
                long toTicks = (long)(to - ChromeEpoch).TotalMicroseconds;

                var results = new List<BrowserHistoryEntry>();
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = tempPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT title, url, last_visit_time
                    FROM urls
                    WHERE last_visit_time >= @from AND last_visit_time <= @to
                    GROUP BY title
                    ORDER BY last_visit_time DESC
                    """;
                command.Parameters.AddWithValue("@from", fromTicks);
                command.Parameters.AddWithValue("@to", toTicks);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var title = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var url = reader.GetString(1);
                    var lastVisitTime = ChromeEpoch.AddMicroseconds(reader.GetInt64(2)).ToLocalTime();

                    results.Add(new BrowserHistoryEntry
                    {
                        Title = title,
                        Url = url,
                        LastVisitTime = lastVisitTime
                    });
                }

                return results;
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private static async Task<List<BrowserHistoryEntry>> GetFirefoxHistoryAsync(string dbPath, DateTime from, DateTime to)
        {
            if (!File.Exists(dbPath))
                return [];

            var tempPath = Path.GetTempFileName();
            try
            {
                File.Copy(dbPath, tempPath, overwrite: true);

                long fromMicros = (long)(from - UnixEpoch).TotalMicroseconds;
                long toMicros = (long)(to - UnixEpoch).TotalMicroseconds;

                var results = new List<BrowserHistoryEntry>();
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = tempPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT p.title, p.url, MAX(h.visit_date) AS last_visit
                    FROM moz_historyvisits h
                    JOIN moz_places p ON h.place_id = p.id
                    WHERE h.visit_date >= @from AND h.visit_date <= @to
                    GROUP BY p.title
                    ORDER BY last_visit DESC
                    """;
                command.Parameters.AddWithValue("@from", fromMicros);
                command.Parameters.AddWithValue("@to", toMicros);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var title = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    var url = reader.GetString(1);
                    var lastVisitTime = UnixEpoch.AddMicroseconds(reader.GetInt64(2)).ToLocalTime();

                    results.Add(new BrowserHistoryEntry
                    {
                        Title = title,
                        Url = url,
                        LastVisitTime = lastVisitTime
                    });
                }

                return results;
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}