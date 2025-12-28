using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorDashboardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAuditionVideo",
                table: "Videos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForRecruiting",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileViews",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Students",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByStaffId",
                table: "ScholarshipOffer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByDirectorId",
                table: "ScholarshipOffer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeniedByDirectorId",
                table: "ScholarshipOffer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorApprovalDate",
                table: "ScholarshipOffer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorApprovalNotes",
                table: "ScholarshipOffer",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorApprovalReason",
                table: "ScholarshipOffer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorApprovalStatus",
                table: "ScholarshipOffer",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDirectorApproval",
                table: "ScholarshipOffer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAllocation",
                table: "BandStaff",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipOffer_ApprovedByDirectorId",
                table: "ScholarshipOffer",
                column: "ApprovedByDirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipOffer_DeniedByDirectorId",
                table: "ScholarshipOffer",
                column: "DeniedByDirectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_ApprovedByDirectorId",
                table: "ScholarshipOffer",
                column: "ApprovedByDirectorId",
                principalTable: "BandStaff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_DeniedByDirectorId",
                table: "ScholarshipOffer",
                column: "DeniedByDirectorId",
                principalTable: "BandStaff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_ApprovedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_DeniedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropIndex(
                name: "IX_ScholarshipOffer_ApprovedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropIndex(
                name: "IX_ScholarshipOffer_DeniedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "IsAuditionVideo",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsAvailableForRecruiting",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProfileViews",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ApprovedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "DeniedByDirectorId",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "DirectorApprovalDate",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "DirectorApprovalNotes",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "DirectorApprovalReason",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "DirectorApprovalStatus",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "RequiresDirectorApproval",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "BudgetAllocation",
                table: "BandStaff");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByStaffId",
                table: "ScholarshipOffer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
