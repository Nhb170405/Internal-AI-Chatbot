using Microsoft.Extensions.Options;
using backend_dotnet.Infrastructure.Errors;

namespace backend_dotnet.Modules.Documents;

public sealed class FileValidationService
{
    private readonly FileValidationOptions _options;

    public FileValidationService(IOptions<FileValidationOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateUpload(IFormFile file)
    {
        // Bai tap Milestone 16 - Upload hardening:
        // Ham nay la cong vao chinh de validate file upload.
        //
        // Thu tu nen lam:
        // 1. Neu file == null thi throw ValidationApiException("invalid_file", "File is required.").
        // 2. Goi EnsureValidSize(file.Length).
        // 3. Lay safeOriginalFileName = GetSafeOriginalFileName(file.FileName).
        // 4. Lay extension = GetNormalizedExtension(safeOriginalFileName).
        // 5. Goi EnsureAllowedExtension(extension).
        // 6. Goi EnsureAllowedContentType(file.ContentType).
        // 7. Goi EnsureExtensionMatchesContentType(extension, file.ContentType).
        //
        // Ket qua:
        // - File hop le: ham khong return gi.
        // - File khong hop le: throw ValidationApiException voi code/message an toan.
        //
        // Luu y:
        // - Khong doc noi dung file o day.
        // - Khong luu file o day.
        // - Khong dung file.FileName de tao duong dan luu file.
        if (file == null)
        {
            throw new ValidationApiException("invalid_file", "File is required.");
        }

        EnsureValidSize(file.Length);

        var safeOriginalFileName = GetSafeOriginalFileName(file.FileName);

        var extension = GetNormalizedExtension(safeOriginalFileName);

        EnsureAllowedExtension(extension);

        EnsureAllowedContentType(file.ContentType);

        EnsureExtensionMatchesContentType(extension, file.ContentType);
    }

    public string GetNormalizedExtension(string fileName)
    {
        // Bai tap:
        // 1. Dung Path.GetExtension(fileName) de lay extension.
        // 2. Trim va ToLowerInvariant de normalize.
        // 3. Neu extension rong thi throw ValidationApiException("invalid_file", "File extension is required.").
        // 4. Return extension da normalize.
        var extention = Path.GetExtension(fileName).Trim();

        if (string.IsNullOrWhiteSpace(extention))
        {
            throw new ValidationApiException("invalid_file", "File extension is required.");
        }

        return extention;
    }

    public string GetSafeOriginalFileName(string fileName)
    {
        // Bai tap:
        // 1. Dung Path.GetFileName(fileName) de loai path traversal.
        // 2. Trim ket qua.
        // 3. Neu rong thi throw ValidationApiException("invalid_file", "File name is required.").
        // 3. Return ten file an toan de luu metadata.
        //
        // Luu y:
        // - Ten nay chi de hien thi.
        // - StoredFileName van nen dung documentId + extension.
        var name = Path.GetFileName(fileName).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationApiException("invalid_file", "File name is required.");
        }

        return name;
    }

    public void EnsureValidSize(long sizeBytes)
    {
        // Bai tap:
        // 1. Neu sizeBytes <= 0 thi throw ValidationApiException("invalid_file", "File is empty.").
        // 2. Neu sizeBytes > _options.MaxFileSizeBytes thi throw ValidationApiException("file_too_large", "...").
        // 3. Neu hop le thi khong return gi.

        if (sizeBytes <= 0)
        {
            throw new ValidationApiException("invalid_file", "File is empty.");
        }

        if (sizeBytes > _options.MaxFileSizeBytes)
        {
            throw new ValidationApiException(
                "file_too_large",
                $"File is too large. Max allowed size is {_options.MaxFileSizeBytes} bytes.");
        }
    }

    public void EnsureAllowedExtension(string extension)
    {
        // Bai tap:
        // 1. Neu extension rong thi throw ValidationApiException("invalid_file", "File extension is required.").
        // 2. Neu extension nam trong _options.BlockedExtensions thi throw ValidationApiException("unsupported_file_type", "...").
        // 3. Neu extension khong nam trong _options.AllowedExtensions thi throw ValidationApiException("unsupported_file_type", "...").
        // 4. Neu hop le thi khong return gi.
        //
        // Goi y:
        // - So sanh bang StringComparer.OrdinalIgnoreCase de khong bi loi .PDF/.pdf.

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ValidationApiException("invalid_file", "File extension is required.");
        }

        if (_options.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationApiException("unsupported_file_type", "This file type is not allowed.");
        }

        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationApiException("unsupported_file_type", "Unsupported file type.");
        }
    }

    public void EnsureAllowedContentType(string? contentType)
    {
        // Bai tap:
        // 1. Neu contentType rong thi cho qua hoac coi la "application/octet-stream".
        // 2. Normalize bang Trim().
        // 3. Neu contentType khong nam trong _options.AllowedContentTypes thi throw ValidationApiException.
        //
        // Luu y:
        // - Content-Type do client gui len, khong duoc tin tuyet doi.
        // - Day chi la lop check bo sung, khong thay the extension whitelist.
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return;
        }

        var normalizedContentType = contentType.Trim();

        if (!_options.AllowedContentTypes.Contains(
                normalizedContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationApiException(
                "unsupported_content_type",
                "Unsupported content type.");
        }
    }

    public void EnsureExtensionMatchesContentType(string extension, string? contentType)
    {
        // Bai tap nang cao:
        // 1. Neu contentType rong hoac la application/octet-stream thi co the cho qua.
        // 2. Neu extension la .pdf thi contentType nen la application/pdf.
        // 3. Neu extension la .docx thi contentType nen la application/vnd.openxmlformats-officedocument.wordprocessingml.document.
        // 4. Neu extension la .xlsx thi contentType nen la application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.
        // 5. Neu extension la .csv thi contentType co the la text/csv hoac application/vnd.ms-excel.
        // 6. Neu mismatch ro rang thi throw ValidationApiException("file_type_mismatch", "...").
        //
        // Luu y:
        // - Browser/Swagger/Postman co the gui content type khac nhau.
        // - Neu check qua chat se lam dev/test kho chiu, nen chi chan mismatch that ro.

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return;
        }

        var normalizedContentType = contentType.Trim();

        if (normalizedContentType.Equals(
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var normalizedExtension = extension.Trim().ToLowerInvariant();

        string[]? expectedContentTypes = normalizedExtension switch
        {
            ".pdf" => ["application/pdf"],

            ".txt" => ["text/plain"],

            ".csv" => ["text/csv", "application/vnd.ms-excel"],

            ".docx" =>
            [
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            ],

            ".xlsx" =>
            [
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
            ],

            ".xls" => ["application/vnd.ms-excel"],

            _ => null
        };

        if (expectedContentTypes == null)
        {
            return;
        }

        if (!expectedContentTypes.Contains(
                normalizedContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationApiException(
                "file_type_mismatch",
                "File extension does not match content type.");
        }
    }
}
