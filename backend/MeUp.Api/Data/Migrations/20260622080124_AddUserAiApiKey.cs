using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeUp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAiApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedAiApiKey",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedAiApiKey",
                table: "AspNetUsers");
        }
    }
}
