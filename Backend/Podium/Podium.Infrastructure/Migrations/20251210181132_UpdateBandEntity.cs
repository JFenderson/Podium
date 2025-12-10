using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBandEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BandStaff_BandStaffPermissions_PermissionsId",
                table: "BandStaff");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_AspNetUsers_RescindedByUserId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_AspNetUsers_RespondedByGuardianUserId",
                table: "Offers");

            migrationBuilder.DropTable(
                name: "BandStaffPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Offers_RescindedByUserId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_RespondedByGuardianUserId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_BandStaff_PermissionsId",
                table: "BandStaff");

            migrationBuilder.DropColumn(
                name: "ResponsedDate",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "PermissionsId",
                table: "BandStaff");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Offers",
                newName: "RespondedByUserId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Bands",
                newName: "BandName");

            migrationBuilder.AlterColumn<string>(
                name: "TranscodingError",
                table: "Videos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailPath",
                table: "Videos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "VideoRatings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterested",
                table: "StudentInterests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Offers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RespondedByGuardianUserId",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RescindedByUserId",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByStaffId",
                table: "Offers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedDate",
                table: "Offers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Offers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RespondedByGuardian",
                table: "Offers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Bands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Bands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsInterested",
                table: "StudentInterests");

            migrationBuilder.DropColumn(
                name: "RespondedByGuardian",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Bands");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Bands");

            migrationBuilder.RenameColumn(
                name: "RespondedByUserId",
                table: "Offers",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "BandName",
                table: "Bands",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "TranscodingError",
                table: "Videos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailPath",
                table: "Videos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "VideoRatings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RespondedByGuardianUserId",
                table: "Offers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RescindedByUserId",
                table: "Offers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByStaffId",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedDate",
                table: "Offers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Offers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponsedDate",
                table: "Offers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermissionsId",
                table: "BandStaff",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BandStaffPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CanContactStudents = table.Column<bool>(type: "bit", nullable: false),
                    CanManageBand = table.Column<bool>(type: "bit", nullable: false),
                    CanManageEvents = table.Column<bool>(type: "bit", nullable: false),
                    CanManageOffers = table.Column<bool>(type: "bit", nullable: false),
                    CanManageStaff = table.Column<bool>(type: "bit", nullable: false),
                    CanRateStudents = table.Column<bool>(type: "bit", nullable: false),
                    CanSendOffers = table.Column<bool>(type: "bit", nullable: false),
                    CanViewStudents = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BandStaffPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BandStaffPermissions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RescindedByUserId",
                table: "Offers",
                column: "RescindedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RespondedByGuardianUserId",
                table: "Offers",
                column: "RespondedByGuardianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BandStaff_PermissionsId",
                table: "BandStaff",
                column: "PermissionsId");

            migrationBuilder.CreateIndex(
                name: "IX_BandStaffPermissions_ApplicationUserId",
                table: "BandStaffPermissions",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BandStaff_BandStaffPermissions_PermissionsId",
                table: "BandStaff",
                column: "PermissionsId",
                principalTable: "BandStaffPermissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_AspNetUsers_RescindedByUserId",
                table: "Offers",
                column: "RescindedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_AspNetUsers_RespondedByGuardianUserId",
                table: "Offers",
                column: "RespondedByGuardianUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
