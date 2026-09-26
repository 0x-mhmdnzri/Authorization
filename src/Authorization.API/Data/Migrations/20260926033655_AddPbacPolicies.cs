using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPbacPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Policies",
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
                    RequiredRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiredDepartments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MinimumClearance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequireSameDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    RequireBusinessHours = table.Column<bool>(type: "boolean", nullable: false),
                    MaxResourceSensitivity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequireDacGrant = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_ResourceType_Action_IsEnabled",
                table: "Policies",
                columns: new[] { "ResourceType", "Action", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Policies");
        }
    }
}
