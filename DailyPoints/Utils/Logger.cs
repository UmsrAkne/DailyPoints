using System;
using System.IO;

namespace DailyPoints.Utils
{
    public static class Logger
    {
        private static string logFilePath;

        public static void Initialize(string logPath)
        {
            logFilePath = Path.Combine(logPath, "log.txt");
        }

        public static void Log(string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";

            Console.WriteLine(line);
            File.AppendAllText(logFilePath, line + Environment.NewLine);
        }
    }
}
