using System.Threading.Tasks;

namespace DailyPoints.Utils
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public sealed class AppSettings
    {
        public string ApiBaseUrl { get; set; } = "http://localhost:18000";

        public bool EnableDebugLog { get; set; }

        public string SshUserName { get; set; }

        [JsonIgnore]
        private static string ConfigPath =>
            Path.Combine(AppContext.BaseDirectory, "app_settings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var settings = new AppSettings();
                settings.Save();
                return settings;
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }

        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, json);
        }

        public async Task SaveAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(this, options);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
    }
}