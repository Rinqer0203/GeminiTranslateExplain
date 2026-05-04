using System.IO;

namespace GeminiTranslateExplain.Services
{
    public static class AppPaths
    {
        public const string AppDirectoryName = "GeminiTranslateExplain";

        public static string RoamingDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDirectoryName);

        public static string LocalDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDirectoryName);

        public static string ConfigFilePath { get; } = Path.Combine(RoamingDataDirectory, "appconfig.json");

        public static string ImageLogDirectory { get; } = Path.Combine(LocalDataDirectory, "log");

        public static string ErrorLogFilePath { get; } = Path.Combine(LocalDataDirectory, "error.log");

        public static void EnsureDataDirectories()
        {
            Directory.CreateDirectory(RoamingDataDirectory);
            Directory.CreateDirectory(LocalDataDirectory);
            Directory.CreateDirectory(ImageLogDirectory);
        }
    }
}
