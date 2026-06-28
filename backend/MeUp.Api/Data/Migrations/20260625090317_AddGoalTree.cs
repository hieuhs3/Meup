using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeUp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "medium");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Goals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Goals",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "year");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentGoalId",
                table: "Goals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Goals",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<DateOnly>(
                name: "TargetDate",
                table: "Goals",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_ParentGoalId",
                table: "Goals",
                column: "ParentGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId_ParentGoalId",
                table: "Goals",
                columns: new[] { "UserId", "ParentGoalId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Goals_ParentGoalId",
                table: "Goals",
                column: "ParentGoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Goals_ParentGoalId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_ParentGoalId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_UserId_ParentGoalId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "ParentGoalId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "Goals");
        }
    }
}
