using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsalmsReading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPlannedReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO ReadingRecords (Id, PsalmId, DateRead, RuleApplied)
                SELECT p.Id, p.PsalmId, p.ScheduledDate, p.RuleApplied
                FROM PlannedReadings p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ReadingRecords r
                    WHERE r.PsalmId = p.PsalmId
                      AND r.DateRead = p.ScheduledDate
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM ReadingRecords
                WHERE Id IN (SELECT Id FROM PlannedReadings);
                """);
        }
    }
}
