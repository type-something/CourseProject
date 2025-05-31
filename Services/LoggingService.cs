using System.Diagnostics;

namespace CourseProject.Services;

public class LoggingService
{
    private readonly string _logPath;

    public LoggingService()
    {
        _logPath = Path.Combine(AppContext.BaseDirectory, "requests.log");
    }

    public async Task LogRequestAsync(string clientIp, string requestLine)
    {
        try
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} {clientIp} {requestLine}";
            await File.AppendAllLinesAsync(_logPath, new[] { logEntry });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log: {ex.Message}");
        }
    }

    public void LogThreadPoolStatus()
    {
        ThreadPool.GetAvailableThreads(out var availWorkers, out var availIO);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIO);
        var usedWorkers = maxWorkers - availWorkers;
        var usedIO = maxIO - availIO;
        Console.WriteLine($"[ThreadPool] Workers: {usedWorkers}/{maxWorkers}, IO: {usedIO}/{maxIO}");
    }

    public void LogOsThreads(string context)
    {
        var count = Process.GetCurrentProcess().Threads.Count;
        Console.WriteLine($"[OS Threads] {context}: {count}");
    }
}