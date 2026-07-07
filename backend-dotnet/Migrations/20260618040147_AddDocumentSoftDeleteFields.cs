using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UploadedByGuestSessionId",
                table: "Documents",
                newName: "DeletedByUserId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Documents",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "DeletedByUserId",
                table: "Documents",
                newName: "UploadedByGuestSessionId");
        }
    }
}
