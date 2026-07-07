using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.BackgroundJobs;
using backend_dotnet.Modules.Chat;
using backend_dotnet.Modules.Datasets;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Sessions;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Infrastructure.Persistence;

// TODO:
// File nay se la DbContext cua Entity Framework Core.
// Hien tai chua viet class ke thua DbContext de tranh loi build khi ban chua cai package EF Core.
//
// Sau khi ban cai cac package:
// - Microsoft.EntityFrameworkCore
// - Microsoft.EntityFrameworkCore.SqlServer
// - Microsoft.EntityFrameworkCore.Design
//
// Hay chuyen file nay thanh:
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<TokenUsage> TokenUsages => Set<TokenUsage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentExtraction> DocumentExtractions => Set<DocumentExtraction>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentMetadata> DocumentMetadatas => Set<DocumentMetadata>();
    public DbSet<DocumentTableProfile> DocumentTableProfiles => Set<DocumentTableProfile>();
    public DbSet<DocumentProcessingJob> DocumentProcessingJobs => Set<DocumentProcessingJob>();
    public DbSet<DocumentProcessingJobLog> DocumentProcessingJobLogs => Set<DocumentProcessingJobLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO:
        // 1. Cau hinh Users.Email la unique.
        // 2. Cau hinh cac field bat buoc: Email, DisplayName, Role.
        // 3. Cau hinh max length cho string quan trong.
        // 4. Cau hinh table name neu muon.

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DepartmentId).IsRequired(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<GuestSession>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SessionKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.ActorUserId).IsRequired(false);
            entity.Property(e => e.ActorGuestSessionId).IsRequired(false);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ResourceId).IsRequired(false).HasMaxLength(255);
            entity.Property(e => e.MetadataJson).IsRequired(false);
            entity.Property(e => e.IpAddress).IsRequired(false).HasMaxLength(45); // IPv6 max length
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.UserId).IsRequired(false);
            entity.Property(e => e.GuestSessionId).IsRequired(false);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.ChatSessionId).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<TokenUsage>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.ChatSessionId).IsRequired();
            entity.Property(e => e.UserId).IsRequired(false);
            entity.Property(e => e.GuestSessionId).IsRequired(false);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PromptTokens).IsRequired(false);
            entity.Property(e => e.CompletionTokens).IsRequired(false);
            entity.Property(e => e.TotalTokens).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Extension).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SizeBytes).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccessLevel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UploadedByUserId).IsRequired(false);
            entity.Property(e => e.ErrorMessage).IsRequired(false).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.DeletedAt).IsRequired(false);
            entity.Property(e => e.DeletedByUserId).IsRequired(false);
        });

        modelBuilder.Entity<DocumentExtraction>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.ExtractedText).IsRequired();
            entity.Property(e => e.ParserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CharacterCount).IsRequired();
            entity.Property(e => e.PageCount).IsRequired(false);
            entity.Property(e => e.MetadataJson).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.DocumentId).IsUnique();

            entity.HasOne(e => e.Document)
                .WithOne()
                .HasForeignKey<DocumentExtraction>(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.ChunkIndex).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CharacterCount).IsRequired();
            entity.Property(e => e.StartOffset).IsRequired(false);
            entity.Property(e => e.EndOffset).IsRequired(false);
            entity.Property(e => e.MetadataJson).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }).IsUnique();

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.Title).IsRequired(false).HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired(false).HasMaxLength(2000);
            entity.Property(e => e.ReportType).IsRequired(false).HasMaxLength(100);
            entity.Property(e => e.ReportDate).IsRequired(false);
            entity.Property(e => e.ReportMonth).IsRequired(false);
            entity.Property(e => e.ReportYear).IsRequired(false);
            entity.Property(e => e.Department).IsRequired(false).HasMaxLength(100);
            entity.Property(e => e.SourceSystem).IsRequired(false).HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired(false).HasMaxLength(20);
            entity.Property(e => e.KeywordsJson).IsRequired(false);
            entity.Property(e => e.TagsJson).IsRequired(false);
            entity.Property(e => e.DetectedColumnsJson).IsRequired(false);
            entity.Property(e => e.SheetNamesJson).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.DocumentId).IsUnique();
            entity.HasIndex(e => new { e.ReportYear, e.ReportMonth });
            entity.HasIndex(e => e.ReportType);
            entity.HasIndex(e => e.Department);

            entity.HasOne(e => e.Document)
                .WithOne()
                .HasForeignKey<DocumentMetadata>(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentTableProfile>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.SheetName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TableIndex).IsRequired();
            entity.Property(e => e.RowCount).IsRequired();
            entity.Property(e => e.ColumnCount).IsRequired();
            entity.Property(e => e.ColumnsJson).IsRequired();
            entity.Property(e => e.SampleRowsJson).IsRequired();
            entity.Property(e => e.WarningsJson).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.DocumentId, e.SheetName, e.TableIndex }).IsUnique();

            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentProcessingJob>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.HangfireJobId).IsRequired(false).HasMaxLength(100);
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.MaxAttempts).IsRequired();
            entity.Property(e => e.LastError).IsRequired(false).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.StartedAt).IsRequired(false);
            entity.Property(e => e.CompletedAt).IsRequired(false);

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentProcessingJobLog>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.DocumentProcessingJobId).IsRequired();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Step).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Attempt).IsRequired();
            entity.Property(e => e.ErrorMessage).IsRequired(false).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.StartedAt).IsRequired(false);
            entity.Property(e => e.CompletedAt).IsRequired(false);

            entity.HasIndex(e => e.DocumentProcessingJobId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentProcessingJobId, e.Step });

            entity.HasOne<DocumentProcessingJob>()
                .WithMany()
                .HasForeignKey(e => e.DocumentProcessingJobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
