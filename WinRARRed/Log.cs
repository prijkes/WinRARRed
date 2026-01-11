using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace WinRARRed
{
    public static class Log
    {
        public static event EventHandler<string>? Logged;

        private static readonly ILogger Logger;

        static Log()
        {
            // Get the directory where the executable is located
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logsDirectory = Path.Combine(exeDirectory, "logs");
            
            // Ensure logs directory exists
            Directory.CreateDirectory(logsDirectory);

            // Configure Serilog
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logsDirectory, "winrarred-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30)
                .WriteTo.Debug()
                .CreateLogger();

            Logger.Information("=== WinRARRed Application Started ===");
        }

        public static void Write(object? sender, string text)
        {
            Information(sender, text);
        }

        public static void Debug(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Debug("[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[DEBUG] {message}");
        }

        public static void Information(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Information("[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[INFO] {message}");
        }

        public static void Warning(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Warning("[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[WARNING] {message}");
        }

        public static void Error(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Error("[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[ERROR] {message}");
        }

        public static void Error(object? sender, Exception exception, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Error(exception, "[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[ERROR] {message}: {exception.Message}");
        }

        public static void Fatal(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Fatal("[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[FATAL] {message}");
        }

        public static void Fatal(object? sender, Exception exception, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Fatal(exception, "[{Sender}] {Message}", senderName, message);
            Logged?.Invoke(sender, $"[FATAL] {message}: {exception.Message}");
        }

        public static void Verbose(object? sender, string message)
        {
            string senderName = sender?.GetType().Name ?? "Unknown";
            Logger.Verbose("[{Sender}] {Message}", senderName, message);
            // Don't fire event for verbose as it may be too noisy
        }

        public static void CloseAndFlush()
        {
            Logger.Information("=== WinRARRed Application Shutting Down ===");
            Serilog.Log.CloseAndFlush();
        }
    }
}
