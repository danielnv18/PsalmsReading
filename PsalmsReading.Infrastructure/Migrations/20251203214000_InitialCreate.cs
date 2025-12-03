using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsalmsReading.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Epigraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Epigraphs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlannedReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PsalmId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RuleApplied = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Psalms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TotalVerses = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Psalms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PsalmId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateRead = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PsalmEpigraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PsalmId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpigraphId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsalmEpigraphs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsalmEpigraphs_Epigraphs_EpigraphId",
                        column: x => x.EpigraphId,
                        principalTable: "Epigraphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsalmEpigraphs_Psalms_PsalmId",
                        column: x => x.PsalmId,
                        principalTable: "Psalms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsalmThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PsalmId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsalmThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsalmThemes_Psalms_PsalmId",
                        column: x => x.PsalmId,
                        principalTable: "Psalms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsalmThemes_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Epigraphs_Name",
                table: "Epigraphs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedReadings_ScheduledDate_PsalmId",
                table: "PlannedReadings",
                columns: new[] { "ScheduledDate", "PsalmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsalmEpigraphs_EpigraphId",
                table: "PsalmEpigraphs",
                column: "EpigraphId");

            migrationBuilder.CreateIndex(
                name: "IX_PsalmEpigraphs_PsalmId_EpigraphId",
                table: "PsalmEpigraphs",
                columns: new[] { "PsalmId", "EpigraphId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsalmThemes_PsalmId_ThemeId",
                table: "PsalmThemes",
                columns: new[] { "PsalmId", "ThemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsalmThemes_ThemeId",
                table: "PsalmThemes",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingRecords_PsalmId_DateRead",
                table: "ReadingRecords",
                columns: new[] { "PsalmId", "DateRead" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Themes_Name",
                table: "Themes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedReadings");

            migrationBuilder.DropTable(
                name: "PsalmEpigraphs");

            migrationBuilder.DropTable(
                name: "PsalmThemes");

            migrationBuilder.DropTable(
                name: "ReadingRecords");

            migrationBuilder.DropTable(
                name: "Epigraphs");

            migrationBuilder.DropTable(
                name: "Psalms");

            migrationBuilder.DropTable(
                name: "Themes");
        }
    }
}
