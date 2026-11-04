using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodAssistant.Migrations
{
    /// <inheritdoc />
    public partial class ThirdMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthyAlternativesJson",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IngredientName",
                table: "RecipeIngredients",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthyAlternativesJson",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IngredientName",
                table: "RecipeIngredients");
        }
    }
}
