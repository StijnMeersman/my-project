using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace my_project.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyTimeGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeekRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeekRows_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeekRows_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_PersonId_Date",
                table: "TimeEntries",
                columns: new[] { "PersonId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_WeekRows_PersonId_WeekStartDate_ProjectId",
                table: "WeekRows",
                columns: new[] { "PersonId", "WeekStartDate", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeekRows_ProjectId",
                table: "WeekRows",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeekRows");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_PersonId_Date",
                table: "TimeEntries");
        }
    }
}
