using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeletedAt",
                table: "Departments",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departments_DeletedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Departments");
        }
    }
}
