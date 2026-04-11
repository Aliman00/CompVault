using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTypeCreatedByFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_CreatedById",
                table: "DocumentTypes",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_AspNetUsers_CreatedById",
                table: "DocumentTypes",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_AspNetUsers_CreatedById",
                table: "DocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTypes_CreatedById",
                table: "DocumentTypes");
        }
    }
}
