namespace FoodAssistant.DTOs.Recipes
{
    public class NutritionSummaryDto
    {
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }

        public decimal CaloriesPerServing { get; set; }
        public decimal ProteinPerServing { get; set; }
        public decimal CarbsPerServing { get; set; }
        public decimal FatPerServing { get; set; }
    }
}
