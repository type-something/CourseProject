using System.Net;
using System.Net.Sockets;
using System.Text;
using CourseProject.Services;
using System.IO;

namespace CourseProject.Services;

public class HttpHandler
{
    private readonly FileService _fileService;
    private readonly LoggingService _loggingService;
    private int _activeHandlers;

    public HttpHandler(FileService fileService, LoggingService loggingService)
    {
        _fileService = fileService;
        _loggingService = loggingService;
    }

    public async Task HandleClientAsync(TcpClient client)
    {
        Interlocked.Increment(ref _activeHandlers);
        // Console.WriteLine($"[Handlers] {_activeHandlers}");

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var requestLine = await reader.ReadLineAsync();
            if (requestLine == null) return;

            var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
            await _loggingService.LogRequestAsync(clientIp, requestLine);

            var parts = requestLine.Split(' ', 3);
            if (parts.Length != 3) return;

            var (method, rawPath, version) = (parts[0], parts[1], parts[2]);
            var path = WebUtility.UrlDecode(rawPath);

            if (method != "GET")
            {
                await WriteErrorResponseAsync(stream, 405, "Method Not Allowed", path);
                return;
            }

            if (path.Contains(".."))
            {
                await WriteErrorResponseAsync(stream, 403, "Forbidden", path);
                return;
            }

            try
            {
                if (!_fileService.IsAllowedExtension(path))
                {
                    await WriteErrorResponseAsync(stream, 403, "Forbidden", path);
                    return;
                }

                var fileBytes = await _fileService.ReadFileAsync(path);
                var contentType = _fileService.GetContentType(path);
                var headers = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {fileBytes.Length}\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(headers));
                // Console.WriteLine("here 2");
                await stream.WriteAsync(fileBytes);
            }
            catch (FileNotFoundException)
            {
                await WriteErrorResponseAsync(stream, 404, "Not Found", path);
            }
            catch (UnauthorizedAccessException)
            {
                await WriteErrorResponseAsync(stream, 403, "Forbidden", path);
            }
        }
        finally
        {
            client.Dispose();
            Interlocked.Decrement(ref _activeHandlers);
            // Console.WriteLine($"[Handlers] {_activeHandlers}");
        }
    }

    private async Task WriteErrorResponseAsync(NetworkStream stream, int code, string text, string path)
    {
        var contentType = "text/html; charset=utf-8";
        var errorHtml = await _fileService.ReadErrorPageAsync(code, text);
        var bodyBytes = Encoding.UTF8.GetBytes(errorHtml);

        var headers =
            $"HTTP/1.1 {code} {text}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        await stream.WriteAsync(Encoding.UTF8.GetBytes(headers));
        await stream.WriteAsync(bodyBytes);
    }
}