using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsalmsReading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropPlannedReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannedReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PsalmId = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleApplied = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedReadings_ScheduledDate_PsalmId",
                table: "PlannedReadings",
                columns: new[] { "ScheduledDate", "PsalmId" },
                unique: true);
        }
    }
}
