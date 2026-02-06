using System;
using System.IO;
using Serilog;

namespace WinRARRed;

/// <summary>
/// Specifies the target log panel for displaying log messages in the UI.
/// </summary>
public enum LogTarget
{
    /// <summary>
    /// General system log messages.
    /// </summary>
    System,

    /// <summary>
    /// Log messages related to Phase 1 (comment block brute-force).
    /// </summary>
    Phase1,

    /// <summary>
    /// Log messages related to Phase 2 (full RAR brute-force).
    /// </summary>
    Phase2
}

/// <summary>
/// Provides data for log events, including the message text and target log panel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LogEventArgs"/> class.
/// </remarks>
/// <param name="message">The log message text.</param>
/// <param name="target">The target log panel. Defaults to <see cref="LogTarget.System"/>.</param>
public class LogEventArgs(string message, LogTarget target = LogTarget.System) : EventArgs
{
    /// <summary>
    /// Gets the log message text.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the target log panel for this message.
    /// </summary>
    public LogTarget Target { get; } = target;
}

/// <summary>
/// Provides centralized logging functionality using Serilog with file and debug output.
/// Raises events for UI log display integration.
/// </summary>
public static class Log
{
    /// <summary>
    /// Occurs when a log message is written. Subscribe to display messages in the UI.
    /// </summary>
    public static event EventHandler<LogEventArgs>? Logged;

    private static readonly Serilog.Core.Logger Logger;

    /// <summary>
    /// The timestamp when the application started, used in log filenames.
    /// </summary>
    public static readonly DateTime StartupTime = DateTime.Now;

    static Log()
    {
        // Get the directory where the executable is located
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string logsDirectory = Path.Combine(exeDirectory, "logs");

        // Ensure logs directory exists
        Directory.CreateDirectory(logsDirectory);

        // Generate log filename with startup timestamp (e.g., winrarred-2026-02-02_14-30-45.log)
        string startupTimestamp = StartupTime.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFileName = $"winrarred-{startupTimestamp}.log";

        // Configure Serilog
        Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logsDirectory, logFileName),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();

        Logger.Information("=== WinRARRed Application Started ===");
    }

    /// <summary>
    /// Writes an informational log message. Alias for <see cref="Information"/>.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="text">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Write(object? sender, string text, LogTarget target = LogTarget.System)
    {
        Information(sender, text, target);
    }

    /// <summary>
    /// Writes a debug-level log message.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Debug(object? sender, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Debug("[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[DEBUG] {message}", target));
    }

    /// <summary>
    /// Writes an informational log message.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Information(object? sender, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Information("[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[INFO] {message}", target));
    }

    /// <summary>
    /// Writes a warning-level log message.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Warning(object? sender, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Warning("[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[WARNING] {message}", target));
    }

    /// <summary>
    /// Writes an error-level log message.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Error(object? sender, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Error("[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[ERROR] {message}", target));
    }

    /// <summary>
    /// Writes an error-level log message with exception details.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Error(object? sender, Exception exception, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Error(exception, "[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[ERROR] {message}: {exception.Message}", target));
    }

    /// <summary>
    /// Writes a fatal-level log message.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Fatal(object? sender, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Fatal("[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[FATAL] {message}", target));
    }

    /// <summary>
    /// Writes a fatal-level log message with exception details.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel.</param>
    public static void Fatal(object? sender, Exception exception, string message, LogTarget target = LogTarget.System)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Fatal(exception, "[{Sender}] {Message}", senderName, message);
        Logged?.Invoke(sender, new LogEventArgs($"[FATAL] {message}: {exception.Message}", target));
    }

    /// <summary>
    /// Writes a verbose-level log message. Does not raise the <see cref="Logged"/> event.
    /// </summary>
    /// <param name="sender">The source object of the log message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="target">The target log panel (not used for verbose logging).</param>
    public static void Verbose(object? sender, string message)
    {
        string senderName = sender?.GetType().Name ?? "Unknown";
        Logger.Verbose("[{Sender}] {Message}", senderName, message);
        // Don't fire event for verbose as it may be too noisy
    }

    /// <summary>
    /// Flushes all pending log entries and closes the logger. Call during application shutdown.
    /// </summary>
    public static void CloseAndFlush()
    {
        Logger.Information("=== WinRARRed Application Shutting Down ===");
        Serilog.Log.CloseAndFlush();
    }
}
