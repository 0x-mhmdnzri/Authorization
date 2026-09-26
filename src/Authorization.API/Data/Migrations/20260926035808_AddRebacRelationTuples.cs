using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRebacRelationTuples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelationTuples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ObjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Relation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationTuples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelationTuples_ObjectType_ObjectId",
                table: "RelationTuples",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationTuples_ObjectType_ObjectId_Relation_Subject",
                table: "RelationTuples",
                columns: new[] { "ObjectType", "ObjectId", "Relation", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelationTuples_Subject",
                table: "RelationTuples",
                column: "Subject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelationTuples");
        }
    }
}
