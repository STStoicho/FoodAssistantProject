using System.ComponentModel.DataAnnotations;

namespace FoodAssistant.ViewModels
{
    public class RecipeGeneratorViewModel
    {
        [Required(ErrorMessage = "Въведете продуктите.")]
        public string Ingredients { get; set; } = string.Empty;

        [Range(100, 3000, ErrorMessage = "Калориите трябва да са между 100 и 3000.")]
        public int TargetCalories { get; set; } = 500;

        [Range(1, 10, ErrorMessage = "Броят порции трябва да е между 1 и 10.")]
        public int Servings { get; set; } = 2;
    }
}
