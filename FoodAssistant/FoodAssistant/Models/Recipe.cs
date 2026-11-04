using System.ComponentModel.DataAnnotations;

namespace FoodAssistant.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Servings { get; set; }

        public int PreparationTimeMinutes { get; set; }

        [Required]
        public string Instructions { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Продуктите, които участват в рецептата
        public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    }
}
