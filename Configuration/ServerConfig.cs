namespace CourseProject.Configuration;

public class ServerConfig
{
    public int Port { get; set; } = 8080;
    public string WebRootPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "webroot");
}