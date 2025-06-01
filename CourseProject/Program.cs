using System.Net;
using System.Net.Sockets;
using CourseProject.Configuration;
using CourseProject.Services;

var config = new ServerConfig();
var fileService = new FileService(config.WebRootPath);
var loggingService = new LoggingService();
var httpHandler = new HttpHandler(fileService, loggingService);

var poolMonitor = new Timer(_ =>
{
    // Console.WriteLine("here");
    loggingService.LogThreadPoolStatus();
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

var listener = new TcpListener(IPAddress.Any, config.Port);
listener.Start();
Console.WriteLine($"Server on {config.Port}, root {Path.GetFullPath(config.WebRootPath)}");
loggingService.LogOsThreads("startup");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => httpHandler.HandleClientAsync(client));
}
