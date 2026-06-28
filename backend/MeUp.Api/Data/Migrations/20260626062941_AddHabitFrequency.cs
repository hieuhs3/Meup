using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeUp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "Habits",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "daily");

            migrationBuilder.AddColumn<int>(
                name: "TargetPerWeek",
                table: "Habits",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "TargetPerWeek",
                table: "Habits");
        }
    }
}
