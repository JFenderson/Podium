using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContextEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_AspNetUsers_ApprovedByUserId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_BandStaff_CreatedByStaffId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Bands_BandId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Students_StudentId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Students_Instrument",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_State_Instrument",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Offers",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Instrument",
                table: "Students");

            migrationBuilder.RenameTable(
                name: "Offers",
                newName: "ScholarshipOffer");

            migrationBuilder.RenameIndex(
                name: "IX_Offers_StudentId",
                table: "ScholarshipOffer",
                newName: "IX_ScholarshipOffer_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_Offers_CreatedByStaffId",
                table: "ScholarshipOffer",
                newName: "IX_ScholarshipOffer_CreatedByStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_Offers_BandId",
                table: "ScholarshipOffer",
                newName: "IX_ScholarshipOffer_BandId");

            migrationBuilder.RenameIndex(
                name: "IX_Offers_ApprovedByUserId",
                table: "ScholarshipOffer",
                newName: "IX_ScholarshipOffer_ApprovedByUserId");

            migrationBuilder.AlterColumn<int>(
                name: "YearsExperience",
                table: "Students",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SecondaryInstruments",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryInstrument",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianInviteCode",
                table: "Students",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StudentId1",
                table: "StudentRatings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ContactRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BandBudgets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScholarshipOffer",
                table: "ScholarshipOffer",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_PrimaryInstrument",
                table: "Students",
                column: "PrimaryInstrument");

            migrationBuilder.CreateIndex(
                name: "IX_Students_State_PrimaryInstrument",
                table: "Students",
                columns: new[] { "State", "PrimaryInstrument" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRatings_StudentId1",
                table: "StudentRatings",
                column: "StudentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_AspNetUsers_ApprovedByUserId",
                table: "ScholarshipOffer",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_CreatedByStaffId",
                table: "ScholarshipOffer",
                column: "CreatedByStaffId",
                principalTable: "BandStaff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_Bands_BandId",
                table: "ScholarshipOffer",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScholarshipOffer_Students_StudentId",
                table: "ScholarshipOffer",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentRatings_Students_StudentId1",
                table: "StudentRatings",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_AspNetUsers_ApprovedByUserId",
                table: "ScholarshipOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_BandStaff_CreatedByStaffId",
                table: "ScholarshipOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_Bands_BandId",
                table: "ScholarshipOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_ScholarshipOffer_Students_StudentId",
                table: "ScholarshipOffer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentRatings_Students_StudentId1",
                table: "StudentRatings");

            migrationBuilder.DropIndex(
                name: "IX_Students_PrimaryInstrument",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_State_PrimaryInstrument",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentRatings_StudentId1",
                table: "StudentRatings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScholarshipOffer",
                table: "ScholarshipOffer");

            migrationBuilder.DropColumn(
                name: "GuardianInviteCode",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "StudentRatings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BandBudgets");

            migrationBuilder.RenameTable(
                name: "ScholarshipOffer",
                newName: "Offers");

            migrationBuilder.RenameIndex(
                name: "IX_ScholarshipOffer_StudentId",
                table: "Offers",
                newName: "IX_Offers_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_ScholarshipOffer_CreatedByStaffId",
                table: "Offers",
                newName: "IX_Offers_CreatedByStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_ScholarshipOffer_BandId",
                table: "Offers",
                newName: "IX_Offers_BandId");

            migrationBuilder.RenameIndex(
                name: "IX_ScholarshipOffer_ApprovedByUserId",
                table: "Offers",
                newName: "IX_Offers_ApprovedByUserId");

            migrationBuilder.AlterColumn<int>(
                name: "YearsExperience",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecondaryInstruments",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryInstrument",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instrument",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Offers",
                table: "Offers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Instrument",
                table: "Students",
                column: "Instrument");

            migrationBuilder.CreateIndex(
                name: "IX_Students_State_Instrument",
                table: "Students",
                columns: new[] { "State", "Instrument" });

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_AspNetUsers_ApprovedByUserId",
                table: "Offers",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_BandStaff_CreatedByStaffId",
                table: "Offers",
                column: "CreatedByStaffId",
                principalTable: "BandStaff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Bands_BandId",
                table: "Offers",
                column: "BandId",
                principalTable: "Bands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Students_StudentId",
                table: "Offers",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
