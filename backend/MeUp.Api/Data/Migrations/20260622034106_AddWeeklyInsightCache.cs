using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeUp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyInsightCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekTo = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyInsights_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyInsights_UserId_WeekFrom_WeekTo",
                table: "WeeklyInsights",
                columns: new[] { "UserId", "WeekFrom", "WeekTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyInsights");
        }
    }
}
