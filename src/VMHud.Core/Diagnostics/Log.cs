using System;
using System.IO;
using System.Text;

namespace VMHud.Core.Diagnostics;

public static class Log
{
    private static readonly object Gate = new();
    private static string _filePath = string.Empty;
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VMHud", "logs");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, $"vmhud-{DateTime.Now:yyyyMMdd}.log");
        _initialized = true;
        Info("Log initialized");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
        => Write("ERR ", ex is null ? message : message + " | " + ex);

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                if (!_initialized) Init();
                File.AppendAllText(_filePath, line, Encoding.UTF8);
            }
        }
        catch { /* never throw from logging */ }
    }
}

