using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurposeBasedAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurposeAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurposeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Allowed = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurposeAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purposes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purposes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourcePurposes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurposeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowedRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresExplicitConsent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourcePurposes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourcePurposes_Purposes_PurposeId",
                        column: x => x.PurposeId,
                        principalTable: "Purposes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourcePurposes_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurposeAccessLogs_UserId_ResourceId_AccessedAt",
                table: "PurposeAccessLogs",
                columns: new[] { "UserId", "ResourceId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Purposes_Code",
                table: "Purposes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourcePurposes_PurposeId",
                table: "ResourcePurposes",
                column: "PurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourcePurposes_ResourceId_PurposeId",
                table: "ResourcePurposes",
                columns: new[] { "ResourceId", "PurposeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurposeAccessLogs");

            migrationBuilder.DropTable(
                name: "ResourcePurposes");

            migrationBuilder.DropTable(
                name: "Purposes");
        }
    }
}
