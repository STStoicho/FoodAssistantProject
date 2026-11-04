namespace FoodAssistant.DTOs.Recipes
{
    public class HealthyAlternativeDto
    {
        public string OriginalIngredientName { get; set; } = string.Empty;

        public string AlternativeName { get; set; } = string.Empty;

        public string UsdaSearchTerm { get; set; } = string.Empty;

        public decimal QuantityGrams { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
