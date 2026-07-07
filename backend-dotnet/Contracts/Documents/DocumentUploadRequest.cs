namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentUploadRequest
{
    // Swagger se render property IFormFile nay thanh nut chon file
    // khi action dung [FromForm] DocumentUploadRequest.
    public IFormFile File { get; set; } = default!;

    // Gia tri hop le:
    // - admin
    // - employee
    // - guest
    public string? AccessLevel { get; set; }
}
