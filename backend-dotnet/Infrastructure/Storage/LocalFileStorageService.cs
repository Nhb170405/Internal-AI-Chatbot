using backend_dotnet.Infrastructure.Errors;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Storage;

public sealed class LocalFileStorageService
{
    private readonly LocalFileStorageOptions _options;
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IOptions<LocalFileStorageOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<string> SaveAsync(IFormFile file, Guid documentId, string extension, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Tao root path tuyet doi tu _environment.ContentRootPath + _options.RootPath.
        // 2. Dam bao thu muc ton tai bang Directory.CreateDirectory.
        // 3. Tao stored file name an toan: $"{documentId}{extension}".
        // 4. Combine thanh full path.
        // 5. Mo FileStream va copy file.CopyToAsync.
        // 6. Return full path hoac relative path theo quyet dinh cua ban.
        //
        // Luu y bao mat:
        // - Khong dung file.FileName de tao path.
        // - Khong cho user truyen path.
        // - Dam bao extension da duoc validate truoc khi goi ham nay.
        if (file == null)
        {
            throw new ValidationApiException("invalid_file", "Missing file.");
        }

        if (!IsAllowedExtension(extension) || !IsAllowedSize(file.Length))
        {
            throw new ValidationApiException("invalid_file", "Unsupported extension or file is too large.");
        }
        var rootPath = Path.Combine(_environment.ContentRootPath, _options.RootPath);

        Directory.CreateDirectory(rootPath);

        var fileName = documentId.ToString() + extension.ToLowerInvariant();
        var fullPath = Path.Combine(rootPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);

        await file.CopyToAsync(stream, cancellationToken);
        return fullPath;
    }

    public bool IsAllowedExtension(string extension)
    {
        // Bai tap:
        // 1. Neu extension rong thi return false.
        // 2. So sanh lower-case voi _options.AllowedExtensions.
        // 3. Return true neu hop le.
        if (string.IsNullOrWhiteSpace(extension)) { return false; }

        foreach (string e in _options.AllowedExtensions)
        {
            if (e.ToLowerInvariant() == extension.ToLowerInvariant()) return true;
        }
        return false;
    }

    public bool IsAllowedSize(long sizeBytes)
    {
        // Bai tap:
        // 1. File phai > 0.
        // 2. File phai <= _options.MaxFileSizeBytes.
        // 3. Return ket qua bool.
        if (sizeBytes <= 0 || sizeBytes > _options.MaxFileSizeBytes) return false;

        return true;
    }
}
