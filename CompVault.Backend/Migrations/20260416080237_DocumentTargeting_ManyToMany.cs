using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class DocumentTargeting_ManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Opprett nye koblingstabeller
            migrationBuilder.CreateTable(
                name: "DocumentDepartments",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDepartments", x => new { x.DocumentId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_DocumentDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentDepartments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentJobTitles",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentJobTitles", x => new { x.DocumentId, x.JobTitleId });
                    table.ForeignKey(
                        name: "FK_DocumentJobTitles_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentJobTitles_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDepartments_DepartmentId",
                table: "DocumentDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentJobTitles_JobTitleId",
                table: "DocumentJobTitles",
                column: "JobTitleId");

            // 2. Migrer eksisterende data fra TargetDepartmentId til DocumentDepartments
            migrationBuilder.Sql(@"
                INSERT INTO ""DocumentDepartments"" (""DocumentId"", ""DepartmentId"")
                SELECT ""Id"", ""TargetDepartmentId""
                FROM ""Documents""
                WHERE ""TargetDepartmentId"" IS NOT NULL
                  AND ""IsActive"" = true
                  AND ""DeletedAt"" IS NULL;
            ");

            // 3. Migrer eksisterende data fra TargetJobTitleId til DocumentJobTitles
            migrationBuilder.Sql(@"
                INSERT INTO ""DocumentJobTitles"" (""DocumentId"", ""JobTitleId"")
                SELECT ""Id"", ""TargetJobTitleId""
                FROM ""Documents""
                WHERE ""TargetJobTitleId"" IS NOT NULL
                  AND ""IsActive"" = true
                  AND ""DeletedAt"" IS NULL;
            ");

            // 4. Fjern gamle kolonner og begrensninger
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Departments_TargetDepartmentId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_JobTitles_TargetJobTitleId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TargetDepartmentId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TargetJobTitleId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TargetDepartmentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TargetJobTitleId",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Legg tilbake gamle kolonner
            migrationBuilder.AddColumn<Guid>(
                name: "TargetDepartmentId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetJobTitleId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TargetDepartmentId",
                table: "Documents",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TargetJobTitleId",
                table: "Documents",
                column: "TargetJobTitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Departments_TargetDepartmentId",
                table: "Documents",
                column: "TargetDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_JobTitles_TargetJobTitleId",
                table: "Documents",
                column: "TargetJobTitleId",
                principalTable: "JobTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 2. Migrer data tilbake fra koblingstabellene (tar første verdi per dokument)
            migrationBuilder.Sql(@"
                UPDATE ""Documents"" d
                SET ""TargetDepartmentId"" = dd.""DepartmentId""
                FROM (
                    SELECT DISTINCT ON (""DocumentId"") ""DocumentId"", ""DepartmentId""
                    FROM ""DocumentDepartments""
                    ORDER BY ""DocumentId"", ""DepartmentId""
                ) dd
                WHERE d.""Id"" = dd.""DocumentId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Documents"" d
                SET ""TargetJobTitleId"" = dj.""JobTitleId""
                FROM (
                    SELECT DISTINCT ON (""DocumentId"") ""DocumentId"", ""JobTitleId""
                    FROM ""DocumentJobTitles""
                    ORDER BY ""DocumentId"", ""JobTitleId""
                ) dj
                WHERE d.""Id"" = dj.""DocumentId"";
            ");

            // 3. Fjern koblingstabellene
            migrationBuilder.DropTable(
                name: "DocumentDepartments");

            migrationBuilder.DropTable(
                name: "DocumentJobTitles");
        }
    }
}