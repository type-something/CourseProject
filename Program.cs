using System.Net;
using System.Net.Sockets;

const int PORT = 8080;

async Task HandleClientAsync(TcpClient client)
{
    Console.WriteLine("Client connected");
    await Task.CompletedTask;
}

var listener = new TcpListener(IPAddress.Any, PORT);
listener.Start();
Console.WriteLine($"Server listening on port {PORT}");



while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(client));
}
