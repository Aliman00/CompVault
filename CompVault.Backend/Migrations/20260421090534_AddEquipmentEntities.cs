using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompVault.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HasSize = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentItems_EquipmentCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EquipmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentIssuances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentIssuances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentIssuances_AspNetUsers_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentIssuances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentIssuances_EquipmentItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "EquipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCategories_DeletedAt",
                table: "EquipmentCategories",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCategories_Name",
                table: "EquipmentCategories",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentIssuances_DeletedAt",
                table: "EquipmentIssuances",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentIssuances_IssuedById",
                table: "EquipmentIssuances",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentIssuances_ItemId",
                table: "EquipmentIssuances",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentIssuances_UserId",
                table: "EquipmentIssuances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_CategoryId",
                table: "EquipmentItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_CategoryId_Name",
                table: "EquipmentItems",
                columns: new[] { "CategoryId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_DeletedAt",
                table: "EquipmentItems",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentIssuances");

            migrationBuilder.DropTable(
                name: "EquipmentItems");

            migrationBuilder.DropTable(
                name: "EquipmentCategories");
        }
    }
}
