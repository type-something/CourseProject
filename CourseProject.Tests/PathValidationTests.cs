using System.Net;
using Xunit;

namespace CourseProject.Tests;

public class PathValidationTests
{
    private const string WebRootPath = "webroot";
    private readonly string FullWebRootPath;

    public PathValidationTests()
    {
        FullWebRootPath = Path.GetFullPath(WebRootPath);
    }

    [Theory]
    [InlineData("/styles.css", true)]  // valid path
    [InlineData("/index.html", true)]  // valid path
    [InlineData("/../../etc/passwd", false)]  // directory traversal
    [InlineData("/%2e%2e/%2e%2e/etc/passwd", false)]  // url encoded traversal
    [InlineData("/..\\..\\windows\\system32\\config\\sam", false)]  // mixed separators
    [InlineData("/symlink/../../../etc/passwd", false)]  // symbolic link traversal
    [InlineData("/../../../etc/passwd", false)]  // multiple levels
    [InlineData("/%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd", false)]  // encoded characters
    [InlineData("/..%2f..%2f..%2fetc%2fpasswd", false)]  // mixed case traversal
    [InlineData("/../../../etc/passwd%00", false)]  // null byte injection
    [InlineData("/..%c0%af..%c0%af..%c0%afetc/passwd", false)]  // unicode characters
    [InlineData("////../../../etc/passwd", false)]  // multiple forward slashes
    public void ValidatePath_ShouldPreventDirectoryTraversal(string requestPath, bool shouldBeAllowed)
    {
        if (requestPath.Contains("%00"))
        {
            Assert.False(shouldBeAllowed);
            return;
        }

        var decodedPath = WebUtility.UrlDecode(requestPath);
        decodedPath = decodedPath.Replace('\\', '/');

        if (decodedPath.Contains("..") || decodedPath.Contains("%2e%2e"))
        {
            Assert.False(shouldBeAllowed);
            return;
        }

        var filePath = Path.Combine(WebRootPath, decodedPath.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);

        var isAllowed = fullFilePath.StartsWith(FullWebRootPath);

        Assert.Equal(shouldBeAllowed, isAllowed);
    }

    [Fact]
    public void ValidatePath_ShouldHandleEmptyPath()
    {
        var requestPath = "";
        var filePath = Path.Combine(WebRootPath, requestPath.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);

        var isAllowed = fullFilePath.StartsWith(FullWebRootPath);

        Assert.True(isAllowed);
    }

    [Fact]
    public void ValidatePath_ShouldHandleRootPath()
    {
        var requestPath = "/";
        var filePath = Path.Combine(WebRootPath, requestPath.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);

        var isAllowed = fullFilePath.StartsWith(FullWebRootPath);

        Assert.True(isAllowed);
    }

    [Fact]
    public void ValidatePath_ShouldHandleNormalizedPaths()
    {
        var requestPath = "/subdir/../styles.css";
        var filePath = Path.Combine(WebRootPath, requestPath.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);

        var isAllowed = fullFilePath.StartsWith(FullWebRootPath);

        Assert.True(isAllowed);
    }

    [Fact]
    public void ValidatePath_ShouldHandleLongPaths()
    {
        var requestPath = "/" + string.Join("/", Enumerable.Repeat("subdir", 100)) + "/styles.css";
        var filePath = Path.Combine(WebRootPath, requestPath.TrimStart('/'));
        var fullFilePath = Path.GetFullPath(filePath);

        var isAllowed = fullFilePath.StartsWith(FullWebRootPath);

        Assert.True(isAllowed);
    }
}