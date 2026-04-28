using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class NewRelationsAfterFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentIssuances_EquipmentItems_ItemId",
                table: "EquipmentIssuances");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentIssuances_EquipmentItems_ItemId",
                table: "EquipmentIssuances",
                column: "ItemId",
                principalTable: "EquipmentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentIssuances_EquipmentItems_ItemId",
                table: "EquipmentIssuances");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentIssuances_EquipmentItems_ItemId",
                table: "EquipmentIssuances",
                column: "ItemId",
                principalTable: "EquipmentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
