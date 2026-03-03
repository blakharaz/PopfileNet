using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable enable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailIdMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Validity",
                table: "Emails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UniqueId",
                table: "Emails",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Emails""
                SET ""Validity"" = (SELECT ""Validity"" FROM ""EmailId"" WHERE ""EmailId"".""Id"" = ""Emails"".""UniqueIdId""),
                    ""UniqueId"" = ""Emails"".""UniqueIdId""
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Emails_EmailId_UniqueIdId",
                table: "Emails");

            migrationBuilder.DropTable(
                name: "EmailId");

            migrationBuilder.DropIndex(
                name: "IX_Emails_UniqueIdId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "UniqueIdId",
                table: "Emails");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 4, 8, 45, 17, 103, DateTimeKind.Utc).AddTicks(1330));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "Emails");

            migrationBuilder.RenameColumn(
                name: "Validity",
                table: "Emails",
                newName: "UniqueIdId");

            migrationBuilder.CreateTable(
                name: "EmailId",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Validity = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailId", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 2, 20, 59, 20, 734, DateTimeKind.Utc).AddTicks(2710));

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UniqueIdId",
                table: "Emails",
                column: "UniqueIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_EmailId_UniqueIdId",
                table: "Emails",
                column: "UniqueIdId",
                principalTable: "EmailId",
                principalColumn: "Id");
        }
    }
}
