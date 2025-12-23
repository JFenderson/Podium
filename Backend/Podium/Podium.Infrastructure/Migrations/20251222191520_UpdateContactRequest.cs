using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContactRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityDate",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "ContactRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeclined",
                table: "ContactRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "IsDeclined",
                table: "ContactRequests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityDate",
                table: "Students",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
