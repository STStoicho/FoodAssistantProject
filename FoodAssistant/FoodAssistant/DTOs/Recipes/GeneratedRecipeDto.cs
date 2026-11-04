namespace FoodAssistant.DTOs.Recipes
{
    public class GeneratedRecipeDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int PreparationTimeMinutes { get; set; }

        public List<GeneratedRecipeIngredientDto> Ingredients { get; set; } = new();

        public List<string> Instructions { get; set; } = new();

        public List<string> HealthyAlternatives { get; set; } = new();
    }

    public class GeneratedRecipeIngredientDto
    {
        public string Name { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}
