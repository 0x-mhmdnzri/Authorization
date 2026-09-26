using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPacPrivilegedAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivilegedActionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElevationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PrivilegeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resource = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegedActionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivilegeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    AllowedRequesterRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApproverRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivilegeElevationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PrivilegeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ApproverNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegeElevationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivilegeElevationRequests_PrivilegeDefinitions_PrivilegeId",
                        column: x => x.PrivilegeId,
                        principalTable: "PrivilegeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegedActionLogs_UserId_PerformedAt",
                table: "PrivilegedActionLogs",
                columns: new[] { "UserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegeDefinitions_Code",
                table: "PrivilegeDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegeElevationRequests_PrivilegeId",
                table: "PrivilegeElevationRequests",
                column: "PrivilegeId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegeElevationRequests_RequesterId_Status",
                table: "PrivilegeElevationRequests",
                columns: new[] { "RequesterId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivilegedActionLogs");

            migrationBuilder.DropTable(
                name: "PrivilegeElevationRequests");

            migrationBuilder.DropTable(
                name: "PrivilegeDefinitions");
        }
    }
}
