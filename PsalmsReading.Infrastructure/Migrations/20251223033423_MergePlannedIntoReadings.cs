using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsalmsReading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergePlannedIntoReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleApplied",
                table: "ReadingRecords",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RuleApplied",
                table: "PlannedReadings",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuleApplied",
                table: "ReadingRecords");

            migrationBuilder.AlterColumn<string>(
                name: "RuleApplied",
                table: "PlannedReadings",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
