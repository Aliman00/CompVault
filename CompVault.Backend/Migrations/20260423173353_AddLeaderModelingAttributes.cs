using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderModelingAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLeader",
                table: "JobTitles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_ManagerId",
                table: "Departments",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_ManagerId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsLeader",
                table: "JobTitles");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Departments");
        }
    }
}
