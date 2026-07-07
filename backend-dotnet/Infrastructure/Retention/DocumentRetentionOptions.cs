namespace backend_dotnet.Infrastructure.Retention;

public sealed class DocumentRetentionOptions
{
    // So ngay giu file vat ly sau khi document bi soft delete.
    // Milestone 4 chi can config skeleton; purge job that se lam o Milestone 12/13.
    public int DeletedFileRetentionDays { get; set; } = 30;
}
