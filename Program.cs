using System.Net;
using System.Net.Sockets;
using System.Text;


const int PORT = 8080;
var webRootPath = AppContext.GetData("WebRootPath") as string ?? "webroot";

async Task HandleClientAsync(TcpClient client)
{
    try
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var requestLine = await reader.ReadLineAsync();
        if (requestLine == null) return;

        var parts = requestLine.Split(' ', 3);
        if (parts.Length != 3) return;

        var (method, path, version) = (parts[0], parts[1], parts[2]);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {method} {path}");

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
    }
}

bool IsAllowedExtension(string path)
{
    var ext = Path.GetExtension(path).ToLower();
    return ext is ".html" or ".css" or ".js";
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

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(client));
}
