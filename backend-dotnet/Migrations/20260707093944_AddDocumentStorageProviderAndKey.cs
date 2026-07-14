using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStorageProviderAndKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Documents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "Documents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "local");

            migrationBuilder.Sql("""
                UPDATE Documents
                SET StorageProvider = 'local'
                WHERE StorageProvider = '' OR StorageProvider IS NULL
            """);

            migrationBuilder.Sql("""
                UPDATE Documents
                SET StorageKey = StoredFileName
                WHERE StorageKey = '' OR StorageKey IS NULL
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "Documents");
        }
    }
}
