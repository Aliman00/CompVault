using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTypeCategoryDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DocumentTypeCategories",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DocumentTypeCategories");
        }
    }
}
