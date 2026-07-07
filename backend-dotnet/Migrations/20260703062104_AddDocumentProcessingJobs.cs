using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProcessingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingJobs_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentProcessingJobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Step = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProcessingJobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingJobLogs_DocumentProcessingJobs_DocumentProcessingJobId",
                        column: x => x.DocumentProcessingJobId,
                        principalTable: "DocumentProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingJobLogs_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobLogs_DocumentId",
                table: "DocumentProcessingJobLogs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobLogs_DocumentProcessingJobId",
                table: "DocumentProcessingJobLogs",
                column: "DocumentProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobLogs_DocumentProcessingJobId_Step",
                table: "DocumentProcessingJobLogs",
                columns: new[] { "DocumentProcessingJobId", "Step" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobs_CreatedAt",
                table: "DocumentProcessingJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobs_DocumentId",
                table: "DocumentProcessingJobs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingJobs_Status",
                table: "DocumentProcessingJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentProcessingJobLogs");

            migrationBuilder.DropTable(
                name: "DocumentProcessingJobs");
        }
    }
}
