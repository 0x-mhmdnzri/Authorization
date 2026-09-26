using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRadacRiskModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskAssessmentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskScore = table.Column<double>(type: "double precision", nullable: false),
                    OperationalNeedScore = table.Column<double>(type: "double precision", nullable: false),
                    Allowed = table.Column<bool>(type: "boolean", nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceRisk = table.Column<double>(type: "double precision", nullable: false),
                    LocationRisk = table.Column<double>(type: "double precision", nullable: false),
                    TimeRisk = table.Column<double>(type: "double precision", nullable: false),
                    AuthStrengthRisk = table.Column<double>(type: "double precision", nullable: false),
                    BehavioralRisk = table.Column<double>(type: "double precision", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    NormalRiskThreshold = table.Column<double>(type: "double precision", nullable: false),
                    MaxAcceptableRisk = table.Column<double>(type: "double precision", nullable: false),
                    CriticalNeedThreshold = table.Column<double>(type: "double precision", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentLogs_UserId_AssessedAt",
                table: "RiskAssessmentLogs",
                columns: new[] { "UserId", "AssessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAssessmentLogs");

            migrationBuilder.DropTable(
                name: "RiskPolicies");
        }
    }
}
