using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCbacContextPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContextEvaluationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Allowed = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MatchedPolicy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContextSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextEvaluationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContextPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Effect = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedHourStart = table.Column<int>(type: "integer", nullable: true),
                    AllowedHourEnd = table.Column<int>(type: "integer", nullable: true),
                    AllowedDaysOfWeek = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequireTrustedNetwork = table.Column<bool>(type: "boolean", nullable: false),
                    RequireManagedDevice = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedCountries = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BlockedCountries = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MinimumAuthMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BlockAnomalousLocation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContextEvaluationLogs_UserId_EvaluatedAt",
                table: "ContextEvaluationLogs",
                columns: new[] { "UserId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContextPolicies_ResourceType_Action_IsEnabled",
                table: "ContextPolicies",
                columns: new[] { "ResourceType", "Action", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContextEvaluationLogs");

            migrationBuilder.DropTable(
                name: "ContextPolicies");
        }
    }
}
