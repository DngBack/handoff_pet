using System;
using Microsoft.Win32;

namespace CodexPet.Windows
{
    internal sealed class PetSettings
    {
        private const string KeyPath = @"Software\CodexPet";
        public const string DefaultMessage = "Đứng dậy tập thể dục thôi!";

        public int IntervalMinutes { get; set; }
        public int DurationSeconds { get; set; }
        public int SpeedPixels { get; set; }
        public string Message { get; set; }
        public bool StartupEnabled { get; set; }

        public static PetSettings Defaults()
        {
            return new PetSettings
            {
                IntervalMinutes = 25,
                DurationSeconds = 120,
                SpeedPixels = 3,
                Message = DefaultMessage,
                StartupEnabled = true
            };
        }

        public static PetSettings Load()
        {
            PetSettings settings = Defaults();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
            {
                if (key == null) return settings;
                settings.IntervalMinutes = ReadNumber(key, "IntervalMinutes", 1, 180, settings.IntervalMinutes);
                settings.DurationSeconds = ReadNumber(key, "DurationSeconds", 10, 600, settings.DurationSeconds);
                settings.SpeedPixels = ReadNumber(key, "SpeedPixels", 0, 20, settings.SpeedPixels);
                object storedMessage = key.GetValue("Message");
                if (storedMessage != null)
                {
                    string value = storedMessage.ToString().Trim();
                    if (value.Length > 0) settings.Message = value.Length > 80 ? value.Substring(0, 80) : value;
                }
                settings.StartupEnabled = ReadNumber(key, "StartupEnabled", 0, 1, 1) == 1;
            }
            return settings;
        }

        private static int ReadNumber(RegistryKey key, string name, int min, int max, int fallback)
        {
            object stored = key.GetValue(name);
            int value;
            if (stored == null || !int.TryParse(stored.ToString(), out value)) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                key.SetValue("IntervalMinutes", IntervalMinutes, RegistryValueKind.DWord);
                key.SetValue("DurationSeconds", DurationSeconds, RegistryValueKind.DWord);
                key.SetValue("SpeedPixels", SpeedPixels, RegistryValueKind.DWord);
                key.SetValue("Message", Message, RegistryValueKind.String);
                key.SetValue("StartupEnabled", StartupEnabled ? 1 : 0, RegistryValueKind.DWord);
            }
        }
    }
}
