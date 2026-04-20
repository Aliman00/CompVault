using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixedNavigaitonPropertiesForDocuemtnJobTitleDocumenntDocumentDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "DocumentDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDepartments_DepartmentId1",
                table: "DocumentDepartments",
                column: "DepartmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentDepartments_Departments_DepartmentId1",
                table: "DocumentDepartments",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentDepartments_Departments_DepartmentId1",
                table: "DocumentDepartments");

            migrationBuilder.DropIndex(
                name: "IX_DocumentDepartments_DepartmentId1",
                table: "DocumentDepartments");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "DocumentDepartments");
        }
    }
}
