using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRubacAccessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Effect = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceIpAllowList = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceIpDenyList = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TimeStartHour = table.Column<int>(type: "integer", nullable: true),
                    TimeEndHour = table.Column<int>(type: "integer", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiredDepartment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiredRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RateLimitPerHour = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleRateCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleRateCounters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_IsEnabled_Priority",
                table: "AccessRules",
                columns: new[] { "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleRateCounters_UserId_RuleId",
                table: "RuleRateCounters",
                columns: new[] { "UserId", "RuleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRules");

            migrationBuilder.DropTable(
                name: "RuleRateCounters");
        }
    }
}
