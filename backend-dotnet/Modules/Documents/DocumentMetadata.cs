namespace backend_dotnet.Modules.Documents;

public sealed class DocumentMetadata
{
    public Guid Id { get; set; }

    // Khoa ngoai tro ve Documents.Id.
    // Moi document nen co toi da mot ban metadata nghiep vu.
    public Guid DocumentId { get; set; }

    // Ten hien thi than thien hon ten file goc.
    // Vi du: "Bao cao doanh thu thang 04/2026".
    public string? Title { get; set; }

    // Mo ta ngan do admin/employee nhap de giup tim kiem va van hanh.
    public string? Description { get; set; }

    // Loai bao cao/tai lieu: revenue, hr_policy, finance_report, maintenance_log...
    public string? ReportType { get; set; }

    // Ngay bao cao neu document dai dien cho mot ngay cu the.
    public DateOnly? ReportDate { get; set; }

    // Thang/nam bao cao, dung nhieu cho file doanh thu, san luong, ton kho.
    public int? ReportMonth { get; set; }

    public int? ReportYear { get; set; }

    // Phong ban hoac nhom nghiep vu lien quan: sales, hr, finance, production...
    public string? Department { get; set; }

    // He thong nguon neu file duoc export tu ERP/CRM/MES/ke toan.
    public string? SourceSystem { get; set; }

    // Ngon ngu chinh cua document: vi, en, mixed, unknown.
    public string? Language { get; set; }

    // JSON array string, vi du: ["doanh thu","sales","2026"].
    public string? KeywordsJson { get; set; }

    // JSON array string, vi du: ["monthly","finance","internal"].
    public string? TagsJson { get; set; }

    // Milestone 11 se cap nhat tu pandas profile cho CSV/XLSX.
    public string? DetectedColumnsJson { get; set; }

    // Milestone 11 se cap nhat tu pandas profile cho XLSX.
    public string? SheetNamesJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Document? Document { get; set; }
}
