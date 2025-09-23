using System;
using System.IO;
using System.Threading.Tasks;

namespace CMLGapp.Services
{
    public static class Logger
    {
        private static  string logFolder = Path.Combine(FileSystem.AppDataDirectory, "Logs");
        private static  string logFilePath = Path.Combine(logFolder, "app_log.txt");

        public static async Task LogAsync(string message)
        {
            try
            {
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}\n";
                await File.AppendAllTextAsync(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to write log: {ex.Message}");
            }
        }

        public static string GetLogFilePath() => logFilePath;
    }
}
