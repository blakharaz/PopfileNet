using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable enable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buckets", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImapServer = table.Column<string>(type: "text", nullable: false),
                    ImapPort = table.Column<int>(type: "integer", nullable: false),
                    ImapUsername = table.Column<string>(type: "text", nullable: false),
                    ImapPassword = table.Column<string>(type: "text", nullable: false),
                    ImapUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    MaxParallelConnections = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BucketId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailFolders_Buckets_BucketId",
                        column: x => x.BucketId,
                        principalTable: "Buckets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UniqueIdId = table.Column<long>(type: "bigint", nullable: true),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ToAddresses = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsHtml = table.Column<bool>(type: "boolean", nullable: false),
                    Folder = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emails_EmailId_UniqueIdId",
                        column: x => x.UniqueIdId,
                        principalTable: "EmailId",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Emails_MailFolders_Folder",
                        column: x => x.Folder,
                        principalTable: "MailFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MailHeader",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailHeader", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailHeader_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "ImapPassword", "ImapPort", "ImapServer", "ImapUseSsl", "ImapUsername", "MaxParallelConnections", "UpdatedAt" },
                values: new object[] { 1, "", 993, "", true, "", 4, new DateTime(2026, 3, 2, 20, 59, 20, 734, DateTimeKind.Utc).AddTicks(2710) });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Folder",
                table: "Emails",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UniqueIdId",
                table: "Emails",
                column: "UniqueIdId");

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_BucketId",
                table: "MailFolders",
                column: "BucketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailHeader_EmailId",
                table: "MailHeader",
                column: "EmailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailHeader");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "EmailId");

            migrationBuilder.DropTable(
                name: "MailFolders");

            migrationBuilder.DropTable(
                name: "Buckets");
        }
    }
}
