using System;
using System.IO;
using InputOverlayUI.Models;
using Newtonsoft.Json;

namespace InputOverlayUI.Services
{
    public class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InputOverlay",
            "settings.json");

        private static readonly string SettingsDirectory = Path.GetDirectoryName(SettingsPath)!;

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsPath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                // Log error and return default settings
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}