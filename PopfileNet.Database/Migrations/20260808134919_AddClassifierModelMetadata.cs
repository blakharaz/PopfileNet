using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClassifierModelMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassifierModels",
                columns: table => new
                {
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrainingSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TrainedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FormatVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassifierModels", x => x.OwnerId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassifierModels");
        }
    }
}
