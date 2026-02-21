using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<LocalFileStorageOptions> options, IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.RootPath.Replace('/', Path.DirectorySeparatorChar);
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, int userId, int? projectId, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid():N}{extension}";
        var projectSegment = projectId.HasValue ? projectId.Value.ToString() : "personal";
        var relativePath = Path.Combine(userId.ToString(), projectSegment, uniqueName);
        var fullPath = Path.Combine(_rootPath, relativePath);
        var fullDir = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(fullDir);
        await using var outputStream = File.Create(fullPath);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, filePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, filePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Document content not found.", filePath);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}
