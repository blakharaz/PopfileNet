using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PopfileNet.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintOnMailFolderBucketId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MailFolders_BucketId",
                table: "MailFolders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_BucketId",
                table: "MailFolders",
                column: "BucketId",
                unique: true);
        }
    }
}