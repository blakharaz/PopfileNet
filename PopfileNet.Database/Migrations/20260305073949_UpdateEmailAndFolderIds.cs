using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailAndFolderIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailFolders_Buckets_BucketId",
                table: "MailFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Emails_MailFolders_Folder",
                table: "Emails");

            migrationBuilder.AlterColumn<string>(
                name: "BucketId",
                table: "MailFolders",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "MailFolders",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Folder",
                table: "Emails",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ImapUid",
                table: "Emails",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Buckets",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_Name",
                table: "MailFolders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ImapUid_Folder",
                table: "Emails",
                columns: new[] { "ImapUid", "Folder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MailFolders_Buckets_BucketId",
                table: "MailFolders",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_MailFolders_Folder",
                table: "Emails",
                column: "Folder",
                principalTable: "MailFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MailFolders_Name",
                table: "MailFolders");

            migrationBuilder.DropIndex(
                name: "IX_Emails_ImapUid_Folder",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "ImapUid",
                table: "Emails");

            migrationBuilder.AlterColumn<Guid>(
                name: "BucketId",
                table: "MailFolders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MailFolders",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "Folder",
                table: "Emails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Buckets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_MailFolders_Buckets_BucketId",
                table: "MailFolders",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_MailFolders_Folder",
                table: "Emails",
                column: "Folder",
                principalTable: "MailFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
