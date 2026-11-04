using System.ComponentModel.DataAnnotations;

namespace FoodAssistant.Models
{
    public class RecipeIngredient
    {
        public int Id { get; set; }

        // Връзка към рецептата
        public int RecipeId { get; set; }

        public Recipe Recipe { get; set; } = null!;

        // Връзка към продукта
        public int IngredientId { get; set; }

        public Ingredient Ingredient { get; set; } = null!;

        // Количество на продукта в грамове
        [Range(0.1, 10000)]
        public decimal QuantityGrams { get; set; }
    }
}
