namespace backend_dotnet.Modules.Documents;

public sealed class Document
{
    public Guid Id { get; set; }

    // Ten file goc do user upload. Chi dung de hien thi, khong dung lam storage path.
    public string OriginalFileName { get; set; } = string.Empty;

    // Ten file an toan do backend tu tao, vi du: {documentId}.pdf.
    public string StoredFileName { get; set; } = string.Empty;




    // Noi file duoc luu: local, azure_blob, ...
    public string StorageProvider { get; set; } = "local";

    // Dinh danh on dinh cua file trong storage.
    // Local: stored file name.
    // Azure Blob: blob name.
    public string StorageKey { get; set; } = string.Empty;

    // Duong dan noi bo de backend tim file. Khong nen expose tuy tien ra frontend.



    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Status { get; set; } = DocumentStatus.Uploaded;

    // Quyen doc cua document:
    // - admin: chi admin doc duoc.
    // - employee: admin va employee doc duoc.
    // - guest: admin, employee va guest doc duoc.
    public string AccessLevel { get; set; } = DocumentAccessLevel.Employee;

    // Employee/admin upload thi set UploadedByUserId.
    public Guid? UploadedByUserId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Soft delete metadata.
    // Khi user/admin xoa document, API chi set Status = Deleted va gan cac field nay.
    // File vat ly van duoc giu trong retention window de co the restore.
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
