using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

const int PORT = 8080;
var webRootPath = AppContext.GetData("WebRootPath") as string ?? "webroot";
int activeHandlers = 0;

var poolMonitor = new Timer(_ =>
{
    ThreadPool.GetAvailableThreads(out var availWorkers, out var availIO);
    ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIO);
    var usedWorkers = maxWorkers - availWorkers;
    var usedIO = maxIO - availIO;
    Console.WriteLine($"[ThreadPool] Workers: {usedWorkers}/{maxWorkers}, IO: {usedIO}/{maxIO}");
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

void LogOsThreads(string context)
{
    var count = Process.GetCurrentProcess().Threads.Count;
    Console.WriteLine($"[OS Threads] {context}: {count}");
}

async Task HandleClientAsync(TcpClient client)
{
    // LogOsThreads("enter handler");
    Interlocked.Increment(ref activeHandlers);
    Console.WriteLine($"[Handlers] {activeHandlers}");

    try
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var requestLine = await reader.ReadLineAsync();
        if (requestLine == null) return;
        var parts = requestLine.Split(' ', 3);
        if (parts.Length != 3) return;
        var (method, path, version) = (parts[0], parts[1], parts[2]);

        if (method != "GET")
        {
            await WriteResponseAsync(stream, 405, "Method Not Allowed");
            return;
        }

        if (path.Contains("..") || !IsAllowedExtension(path))
        {
            await WriteResponseAsync(stream, 403, "Forbidden");
            return;
        }

        var filePath = Path.Combine(webRootPath, path.TrimStart('/'));
        if (!File.Exists(filePath))
        {
            await WriteResponseAsync(stream, 404, "Not Found");
            return;
        }

        var ext = Path.GetExtension(filePath).ToLower();
        var contentType = ext switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            _ => "application/octet-stream"
        };

        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var headers = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {fileBytes.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(headers));
        await stream.WriteAsync(fileBytes);
    }
    finally
    {
        client.Dispose();
        Interlocked.Decrement(ref activeHandlers);
        Console.WriteLine($"[Handlers] {activeHandlers}");
        // LogOsThreads("exit handler");
    }
}

bool IsAllowedExtension(string path)
{
    var ext = Path.GetExtension(path).ToLower();

     string[] allowedExtensions = [".html", ".css", ".js"];
    return allowedExtensions.Contains(ext);
}

async Task WriteResponseAsync(NetworkStream stream, int code, string text)
{
    var body = $"<h1>{code} {text}</h1>";
    var resp = $"HTTP/1.1 {code} {text}\r\nContent-Type: text/html\r\nConnection: close\r\n\r\n{body}";
    await stream.WriteAsync(Encoding.UTF8.GetBytes(resp));
}

var listener = new TcpListener(IPAddress.Any, PORT);
listener.Start();
Console.WriteLine($"Server on {PORT}, root {Path.GetFullPath(webRootPath)}");
LogOsThreads("startup");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    // LogOsThreads("before dispatch");
    _ = Task.Run(() => HandleClientAsync(client));
    // LogOsThreads("after dispatch");
}
