using System.Text;

namespace CourseProject.Services;

public class FileService
{
    private readonly string _webRootPath;
    private readonly string[] _allowedExtensions = [".html", ".css", ".js"];

    public FileService(string webRootPath)
    {
        _webRootPath = webRootPath;
    }

    public string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            _ => "text/html; charset=utf-8"
        };
    }

    public bool IsAllowedExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return _allowedExtensions.Contains(ext);
    }

    public async Task<byte[]> ReadFileAsync(string path)
    {
        var filePath = Path.Combine(_webRootPath, path.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);
        var fullWebRootPath = Path.GetFullPath(_webRootPath);

        if (!fullFilePath.StartsWith(fullWebRootPath))
        {
            throw new UnauthorizedAccessException("Access to file outside web root is not allowed");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<string> ReadErrorPageAsync(int code, string text)
    {
        var errorPagePath = Path.Combine(_webRootPath, "error.html");

        if (File.Exists(errorPagePath))
        {
            var errorHtml = await File.ReadAllTextAsync(errorPagePath);
            return errorHtml.Replace("{{ERROR_CODE}}", code.ToString())
                          .Replace("{{ERROR_TEXT}}", text);
        }

        return $"<h1>{code} - {text}</h1>";
    }
}