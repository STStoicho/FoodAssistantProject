using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodAssistant.Migrations
{
    /// <inheritdoc />
    public partial class SecondMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesPerServing",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsPerServing",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FatPerServing",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinPerServing",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TargetCaloriesPerServing",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaloriesPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CarbsPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "FatPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ProteinPerServing",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TargetCaloriesPerServing",
                table: "Recipes");
        }
    }
}
