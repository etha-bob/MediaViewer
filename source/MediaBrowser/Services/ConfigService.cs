using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Services
{
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("defaultZoomLevel")]
        public int DefaultZoomLevel { get; set; } = 200;

        [JsonPropertyName("cacheSize")]
        public int CacheSize { get; set; } = 500;

        [JsonPropertyName("autoPlayVideos")]
        public bool AutoPlayVideos { get; set; } = false;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "system";

        [JsonPropertyName("lastDirectory")]
        public string LastDirectory { get; set; } = string.Empty;

        [JsonPropertyName("lastSortField")]
        public string LastSortField { get; set; } = "Name";

        [JsonPropertyName("lastSortAscending")]
        public bool LastSortAscending { get; set; } = true;

        [JsonPropertyName("lastFilterType")]
        public string LastFilterType { get; set; } = "All";

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 1200;

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 800;
    }

    public class ConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                }
            }
            catch (Exception)
            {
                // Return default config on any error
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // Silently fail if config can't be saved (e.g., read-only media)
            }
        }
    }
}
