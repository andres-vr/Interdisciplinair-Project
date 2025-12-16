using System.IO;
using System.Text.Json;

namespace InterdisciplinairProject.Services
{
    /// <summary>
    /// Service for managing application settings like last opened show path.
    /// </summary>
    public class AppSettingsService
    {
        private const string SettingsFileName = "appsettings.json";
        private readonly string _settingsPath;

        public AppSettingsService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InterdisciplinairProject"
            );

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            _settingsPath = Path.Combine(appDataFolder, SettingsFileName);
        }

        /// <summary>
        /// Gets or sets the path to the last opened show.
        /// </summary>
        public string? LastShowPath
        {
            get
            {
                var settings = LoadSettings();
                return settings.LastShowPath;
            }
            set
            {
                var settings = LoadSettings();
                settings.LastShowPath = value;
                SaveSettings(settings);
            }
        }

        private AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        private void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private class AppSettings
        {
            public string? LastShowPath { get; set; }
        }
    }
}
