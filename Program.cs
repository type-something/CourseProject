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

string GetContentType(string path)
{
    var ext = Path.GetExtension(path).ToLower();
    return ext switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "application/javascript; charset=utf-8",
        _ => "application/octet-stream"
    };
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
            await WriteResponseAsync(stream, 405, "Method Not Allowed", path);
            return;
        }

        if (path.Contains("..") || !IsAllowedExtension(path))
        {
            await WriteResponseAsync(stream, 403, "Forbidden", path);
            return;
        }

        var filePath = Path.Combine(webRootPath, path.TrimStart('/'));
        if (!File.Exists(filePath))
        {
            await WriteResponseAsync(stream, 404, "Not Found", path);
            return;
        }

        var contentType = GetContentType(path);
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

async Task WriteResponseAsync(NetworkStream stream, int code, string text, string path)
{
    var contentType = GetContentType(path);
    var body = $"<h1>{code} {text}</h1>";
    var bodyBytes = Encoding.UTF8.GetBytes(body);

    var headers = new[]
    {
        $"HTTP/1.1 {code} {text}",
        $"Content-Type: {contentType}",
        $"Content-Length: {bodyBytes.Length}",
        "Connection: close",
        ""
    };

    var headerBytes = Encoding.UTF8.GetBytes(string.Join("\r\n", headers));
    await stream.WriteAsync(headerBytes);
    await stream.WriteAsync(bodyBytes);
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
