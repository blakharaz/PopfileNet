using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderSyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncAt",
                table: "MailFolders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncEmailCount",
                table: "MailFolders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "MailFolders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "MailFolders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "MailFolders");

            migrationBuilder.DropColumn(
                name: "LastSyncEmailCount",
                table: "MailFolders");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "MailFolders");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "MailFolders");
        }
    }
}
