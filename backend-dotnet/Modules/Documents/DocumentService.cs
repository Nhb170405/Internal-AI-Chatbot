using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Documents;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Storage;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Documents;

public sealed class DocumentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly AuditLogService _auditLogService;
    private readonly DocumentMetadataService _documentMetadataService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FileValidationService _fileValidationService;

    public DocumentService(
        AppDbContext db,
        IFileStorageService storage,
        AuditLogService auditLogService,
        DocumentMetadataService documentMetadataService,
        IHttpContextAccessor httpContextAccessor,
        FileValidationService fileValidationService)
    {
        _db = db;
        _storage = storage;
        _auditLogService = auditLogService;
        _documentMetadataService = documentMetadataService;
        _httpContextAccessor = httpContextAccessor;
        _fileValidationService = fileValidationService;
    }

    public async Task<DocumentUploadResponse> UploadAsync(
        IFormFile file,
        string? accessLevel,
        CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Lay current principal bang GetCurrentPrincipal.
        // 2. Check principal da authenticated, neu chua thi throw UnauthorizedAccessException.
        // 3. Validate file:
        //    - file khong null.
        //    - file.Length > 0.
        //    - file.Length khong vuot config trong LocalFileStorageService.
        //    - extension hop le: .pdf, .docx, .xlsx, .csv, .txt.
        // 4. Lay owner:
        //    - employee/admin: UploadedByUserId tu ClaimTypes.NameIdentifier.
        //    - guest: tam thoi can quyet dinh co cho upload khong. Goi y Milestone 4: chi employee/admin upload.
        // 5. Tao documentId = Guid.NewGuid().
        // 6. Goi _storage.SaveAsync(file, documentId, extension, cancellationToken).
        //    - Ham nay tra ve StoredFileResult, gom StoredFileName/StoragePath/StorageKey.
        // 7. Tao Document entity:
        //    - OriginalFileName = Path.GetFileName(file.FileName).
        //    - StoredFileName = storedFile.StoredFileName.
        //    - StoragePath = storedFile.StoragePath trong giai doan local/backward-compatible.
        //    - ContentType = file.ContentType hoac "application/octet-stream" neu rong.
        //    - Extension, SizeBytes, Status = DocumentStatus.Uploaded.
        //    - CreatedAt/UpdatedAt = DateTimeOffset.UtcNow.
        // 8. Tao bien document, Add vao _db.Documents va SaveChangesAsync.
        // 9. Goi DocumentMetadataService.CreateDefaultMetadataForUploadAsync de tao metadata mac dinh.
        // 10. Ghi audit log action = "document_upload", resourceType = "Document".
        // 11. Map sang DocumentUploadResponse va return.
        var principal = GetCurrentPrincipal();

        _fileValidationService.ValidateUpload(file);

        var originalFileName = _fileValidationService.GetSafeOriginalFileName(file.FileName);
        var extension = _fileValidationService.GetNormalizedExtension(originalFileName);

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }


        var role = GetRole(principal);
        if (role != UserRole.Admin && role != UserRole.Employee)
        {
            throw new ForbiddenApiException("forbidden", "Only employee and admin can upload documents.");
        }
        var normalizedAccessLevel = NormalizeAccessLevel(accessLevel, role);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
        if (userId == null) { throw new UnauthorizedApiException("unauthorized", "Unauthorized"); }


        var documentId = Guid.NewGuid();
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        var now = DateTimeOffset.UtcNow;
        var storedFile = await _storage.SaveAsync(file, documentId, extension, cancellationToken);

        var document = new Document
        {
            Id = documentId,
            UploadedByUserId = userId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFile.StoredFileName,
            StoragePath = storedFile.StoragePath,
            StorageProvider = storedFile.StorageProvider,
            StorageKey = storedFile.StorageKey,
            ContentType = contentType,
            Extension = extension,
            SizeBytes = file.Length,
            Status = DocumentStatus.Uploaded,
            AccessLevel = normalizedAccessLevel,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Documents.Add(document);

        await _db.SaveChangesAsync(cancellationToken);

        await _documentMetadataService.CreateDefaultMetadataForUploadAsync(document, cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = documentId.ToString(),
            Action = "document_upload",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                originalFileName,
                extension,
                sizeBytes = file.Length,
                contentType,
                status = DocumentStatus.Uploaded,
                accessLevel = normalizedAccessLevel
            })
        }, cancellationToken);

        return new DocumentUploadResponse
        {
            Id = documentId,
            OriginalFileName = originalFileName,
            Extension = extension,
            SizeBytes = file.Length,
            Status = DocumentStatus.Uploaded,
            AccessLevel = normalizedAccessLevel,
            UploadedAt = now
        }
        ;
    }

    public async Task<List<DocumentListItemResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Lay principal va check authenticated.
        // 2. Lay role hien tai.
        // 3. Tao query _db.Documents.AsQueryable().
        // 4. Neu admin: cho xem tat ca.
        // 5. Neu employee: employee xem document accessLevel employee/guest.
        // 6. Neu guest/anonymous: throw UnauthorizedAccessException hoac forbid theo quyet dinh.
        // 7. Sort CreatedAt descending.
        // 8. Select sang DocumentListItemResponse.
        // 9. ToListAsync(cancellationToken).
        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        var role = GetRole(principal);
        if (role != UserRole.Admin && role != UserRole.Employee && role != UserRole.Guest)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }

        var query = _db.Documents.Where(document => document.Status != DocumentStatus.Deleted).AsQueryable();

        if (role == UserRole.Admin) { }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else if (role == UserRole.Guest)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }
        return await query.OrderByDescending(document => document.CreatedAt).Select(document => new DocumentListItemResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            Extension = document.Extension,
            SizeBytes = document.SizeBytes,
            Status = document.Status,
            AccessLevel = document.AccessLevel,
            HasTableProfile = _db.DocumentTableProfiles.Any(profile => profile.DocumentId == document.Id),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        }).ToListAsync(cancellationToken);
    }

    public async Task<DocumentResponse> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Lay principal va owner hien tai.
        // 2. Query Document theo Id.
        // 3. Check quyen:
        //    - admin duoc xem.
        //    - employee chi xem document minh upload trong Milestone 4.
        // 4. Neu khong thay hoac khong co quyen thi throw KeyNotFoundException.
        // 5. Map sang DocumentResponse.

        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        var role = GetRole(principal);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);

        var query = _db.Documents.Where(document => document.Id == documentId && document.Status != DocumentStatus.Deleted).AsQueryable();
        if (role == UserRole.Admin) { }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document =>
                document.AccessLevel == DocumentAccessLevel.Employee ||
                document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else if (role == UserRole.Guest)
        {
            query = query.Where(document => document.AccessLevel == DocumentAccessLevel.Guest);
        }
        else
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }

        var document = await query.FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        return new DocumentResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            Extension = document.Extension,
            SizeBytes = document.SizeBytes,
            Status = document.Status,
            AccessLevel = document.AccessLevel,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            ErrorMessage = document.ErrorMessage
        };
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Lay principal, check authenticated.
        // 2. Lay role va userId.
        // 3. Query document theo documentId va check owner:
        //    - admin duoc xoa tat ca.
        //    - employee chi duoc xoa document minh upload.
        // 4. Neu khong thay hoac khong co quyen thi throw KeyNotFoundException.
        // 5. Soft delete:
        //    - Status = DocumentStatus.Deleted.
        //    - DeletedAt = DateTimeOffset.UtcNow.
        //    - DeletedByUserId = userId.
        //    - UpdatedAt = now.
        // 6. SaveChangesAsync.
        // 7. Ghi audit log action = "document_delete".
        //
        // Khong xoa file vat ly tai Milestone 4.
        // File vat ly se duoc purge sau retention period bang DeletedDocumentPurgeJob o milestone sau.
        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        var role = GetRole(principal);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
        var query = _db.Documents.Where(document => document.Id == documentId && document.Status != DocumentStatus.Deleted).AsQueryable();

        if (role != UserRole.Admin && role != UserRole.Employee)
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }
        else if (userId is null)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document => document.UploadedByUserId == userId);
        }
        else if (role == UserRole.Admin) { }

        var document = await query.FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        var previousStatus = document.Status;

        var now = DateTimeOffset.UtcNow;

        document.Status = DocumentStatus.Deleted;
        document.DeletedAt = now;
        document.DeletedByUserId = userId;
        document.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = "document_delete",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                originalFileName = document.OriginalFileName,
                extension = document.Extension,
                sizeBytes = document.SizeBytes,
                accessLevel = document.AccessLevel,
                previousStatus = previousStatus,
                deletedAt = now
            })
        }, cancellationToken);
    }

    public async Task<DocumentResponse> RestoreAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Lay principal, check authenticated.
        // 2. Lay role va userId.
        // 3. Query document theo documentId, bao gom document Status = Deleted.
        // 4. Check owner:
        //    - admin duoc restore tat ca.
        //    - employee chi duoc restore document minh upload.
        // 5. Neu khong thay, khong co quyen, hoac file vat ly da bi purge thi throw KeyNotFoundException.
        // 6. Neu Status khong phai Deleted thi co the return DocumentResponse hien tai hoac throw ArgumentException.
        // 7. Restore:
        //    - Status = DocumentStatus.Uploaded trong Milestone 4.
        //    - DeletedAt = null.
        //    - DeletedByUserId = null.
        //    - UpdatedAt = now.
        // 8. SaveChangesAsync.
        // 9. Ghi audit log action = "document_restore".
        // 10. Return DocumentResponse.
        var principal = GetCurrentPrincipal();
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        var role = GetRole(principal);
        var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
        var query = _db.Documents.Where(document => document.Id == documentId && document.Status == DocumentStatus.Deleted).AsQueryable();

        if (role != UserRole.Admin && role != UserRole.Employee)
        {
            throw new ForbiddenApiException("forbidden", "Forbidden");
        }
        else if (userId is null)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized");
        }
        else if (role == UserRole.Employee)
        {
            query = query.Where(document => document.UploadedByUserId == userId);
        }
        else if (role == UserRole.Admin) { }


        var document = await query.FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        var now = DateTimeOffset.UtcNow;

        document.Status = DocumentStatus.Uploaded;
        document.DeletedAt = null;
        document.DeletedByUserId = null;
        document.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ResourceId = document.Id.ToString(),
            Action = "document_restore",
            ResourceType = "Document",
            MetadataJson = JsonSerializer.Serialize(new
            {
                originalFileName = document.OriginalFileName,
                extension = document.Extension,
                sizeBytes = document.SizeBytes,
                accessLevel = document.AccessLevel,
                previousStatus = DocumentStatus.Deleted,
                restoredAt = now,
                restoredToStatus = DocumentStatus.Uploaded
            })
        }, cancellationToken);

        return new DocumentResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            Extension = document.Extension,
            SizeBytes = document.SizeBytes,
            Status = document.Status,
            AccessLevel = document.AccessLevel,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            ErrorMessage = document.ErrorMessage
        };
    }


    ///////////////////////////////////////////////////////////////////////////////////////////
    /// Helper
    ///////////////////////////////////////////////////////////////////////////////////////////

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        // Lay HttpContext.User.
        // Neu HttpContext null thi day la loi pipeline/DI, throw InvalidOperationException.
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        // Doc ClaimTypes.Role.
        // Neu khong co role thi return anonymous.
        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (role == UserRole.Admin)
        {
            return UserRole.Admin;
        }

        if (role == UserRole.Employee)
        {
            return UserRole.Employee;
        }

        if (role == UserRole.Guest)
        {
            return UserRole.Guest;
        }

        return "anonymous";
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        // Doc claim string va parse sang Guid.
        // Neu claim khong co hoac parse fail thi return null.
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string NormalizeAccessLevel(string? accessLevel, string role)
    {
        // Neu client khong gui accessLevel, mac dinh la employee.
        // Admin co the upload admin/employee/guest.
        // Employee chi co the upload employee/guest.
        // Guest/anonymous khong bao gio goi duoc UploadAsync vi da bi chan truoc do.
        var normalized = string.IsNullOrWhiteSpace(accessLevel)
            ? DocumentAccessLevel.Employee
            : accessLevel.Trim().ToLowerInvariant();

        var isKnownLevel =
            normalized == DocumentAccessLevel.Admin ||
            normalized == DocumentAccessLevel.Employee ||
            normalized == DocumentAccessLevel.Guest;

        if (!isKnownLevel)
        {
            throw new ValidationApiException("invalid_access_level", "Invalid document access level.");
        }

        if (role == UserRole.Admin)
        {
            return normalized;
        }

        if (role == UserRole.Employee &&
            (normalized == DocumentAccessLevel.Employee || normalized == DocumentAccessLevel.Guest))
        {
            return normalized;
        }

        throw new ForbiddenApiException("forbidden", "You cannot upload documents with this access level.");
    }
}
