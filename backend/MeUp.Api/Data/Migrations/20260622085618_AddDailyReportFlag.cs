using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeUp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyReportFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DailyReportEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyReportEnabled",
                table: "AspNetUsers");
        }
    }
}
