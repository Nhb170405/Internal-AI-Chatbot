namespace backend_dotnet.Modules.Documents;

public static class DocumentStatus
{
    // Cac trang thai toi thieu trong Milestone 4.
    // Uploaded: file da duoc upload va metadata da luu vao SQL Server.
    // Processing/Indexed/Failed duoc chuan bi truoc cho cac milestone ingestion/RAG sau.
    public const string Uploaded = "uploaded";
    public const string Processing = "processing";
    public const string Extracted = "extracted";
    public const string Chunked = "chunked";
    public const string Indexed = "indexed";
    public const string Failed = "failed";

    // Soft delete: document bi an khoi list/search/RAG nhung file vat ly chua bi xoa ngay.
    // File vat ly se duoc purge boi background job sau retention period.
    public const string Deleted = "deleted";
}
