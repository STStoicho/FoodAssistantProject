using Microsoft.EntityFrameworkCore;
using FoodAssistant.Data;
using FoodAssistant.DTOs.FoodDataCentral;
using FoodAssistant.DTOs.Recipes;
using FoodAssistant.Models;

namespace FoodAssistant.Services
{
    public class NutritionService
    {
        private readonly FoodDataCentralService _foodService;
        private readonly ApplicationDbContext _dbContext;

        public NutritionService(
            FoodDataCentralService foodService,
            ApplicationDbContext dbContext)
        {
            _foodService = foodService;
            _dbContext = dbContext;
        }

        public async Task<NutritionSummaryDto> CalculateNutritionAsync(
            GeneratedRecipeDto recipe,
            int servings)
        {
            if (servings <= 0)
            {
                throw new ArgumentException("Невалиден брой порции.");
            }

            decimal totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalCarbs = 0;
            decimal totalFat = 0;

            foreach (var item in recipe.Ingredients)
            {
                if (item.Quantity <= 0 ||
                    !string.Equals(item.Unit, "g",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Невалидно количество за продукта {item.Name}.");
                }

                var food = await GetIngredientNutritionAsync(
                    item.UsdaSearchTerm);

                decimal multiplier = item.Quantity / 100m;

                totalCalories += food.CaloriesPer100g * multiplier;
                totalProtein += food.ProteinPer100g * multiplier;
                totalCarbs += food.CarbsPer100g * multiplier;
                totalFat += food.FatPer100g * multiplier;
            }

            return new NutritionSummaryDto
            {
                TotalCalories = Math.Round(totalCalories, 2),
                TotalProtein = Math.Round(totalProtein, 2),
                TotalCarbs = Math.Round(totalCarbs, 2),
                TotalFat = Math.Round(totalFat, 2),

                CaloriesPerServing = Math.Round(totalCalories / servings, 2),
                ProteinPerServing = Math.Round(totalProtein / servings, 2),
                CarbsPerServing = Math.Round(totalCarbs / servings, 2),
                FatPerServing = Math.Round(totalFat / servings, 2)
            };
        }

        private async Task<Ingredient> GetIngredientNutritionAsync(
            string searchTerm)
        {
            string normalizedSearchTerm = searchTerm.Trim();

            // Проверяваме дали продуктът вече е записан в базата.
            var cachedIngredient = await _dbContext.Ingredients
                .FirstOrDefaultAsync(i =>
                    i.Name.ToLower() == normalizedSearchTerm.ToLower());

            if (cachedIngredient != null)
            {
                return cachedIngredient;
            }

            // Търсим продукта в USDA.
            var searchResults = await _foodService.SearchFoodsAsync(
                normalizedSearchTerm);

            if (searchResults.Count == 0)
            {
                throw new InvalidOperationException(
                    $"USDA не намери продукт: {normalizedSearchTerm}");
            }

            // Предпочитаме стандартни хранителни данни.
            var selectedFood = searchResults
                .OrderBy(food => GetDataTypePriority(food.DataType))
                .First();

            // Проверяваме дали FDC записът вече е кеширан.
            var existingIngredient = await _dbContext.Ingredients
                .FirstOrDefaultAsync(i =>
                    i.FdcId == selectedFood.FdcId);

            if (existingIngredient != null)
            {
                return existingIngredient;
            }

            // Извличаме подробните хранителни стойности.
            var details = await _foodService.GetFoodDetailsAsync(
                selectedFood.FdcId);

            if (details == null || details.FoodNutrients.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Няма хранителни данни за {normalizedSearchTerm}.");
            }

            var nutrients = details.FoodNutrients;

            decimal? calories = GetCalories(nutrients);

            decimal? protein = GetNutrientValue(
                nutrients,
                "203",
                1003,
                "Protein");

            decimal? carbs = GetNutrientValue(
                nutrients,
                "205",
                1005,
                "Carbohydrate, by difference");

            decimal? fat = GetNutrientValue(
                nutrients,
                "204",
                1004,
                "Total lipid");

            if (calories == null ||
                protein == null ||
                carbs == null ||
                fat == null)
            {
                throw new InvalidOperationException(
                    $"USDA няма всички необходими хранителни стойности за {normalizedSearchTerm}.");
            }

            // Записваме стойностите за 100 грама в SQLite.
            var ingredient = new Ingredient
            {
                Name = normalizedSearchTerm,
                FdcId = details.FdcId,
                CaloriesPer100g = calories.Value,
                ProteinPer100g = protein.Value,
                CarbsPer100g = carbs.Value,
                FatPer100g = fat.Value
            };

            _dbContext.Ingredients.Add(ingredient);

            await _dbContext.SaveChangesAsync();

            return ingredient;
        }

        private static int GetDataTypePriority(string dataType)
        {
            return dataType switch
            {
                "Foundation" => 0,
                "SR Legacy" => 1,
                "Survey (FNDDS)" => 2,
                "Branded" => 3,
                _ => 4
            };
        }

        private static decimal? GetCalories(
            List<FoodNutrientDto> nutrients)
        {
            var matches = nutrients
                .Where(n => n.Nutrient != null &&
                    (
                        n.Nutrient.Id == 2047 ||
                        n.Nutrient.Id == 2048 ||
                        n.Nutrient.Id == 1008 ||
                        n.Nutrient.Number == "208" ||
                        n.Nutrient.Number == "2047" ||
                        n.Nutrient.Number == "2048" ||
                        n.Nutrient.Name.Contains(
                            "Energy",
                            StringComparison.OrdinalIgnoreCase)
                    ))
                .OrderBy(n =>
                    n.Nutrient!.Id == 2047 ? 0 :
                    n.Nutrient.Id == 2048 ? 1 :
                    n.Nutrient.Number == "208" ? 2 : 3)
                .ToList();

            var selected = matches.FirstOrDefault();

            if (selected == null)
            {
                return null;
            }

            if (selected.Nutrient!.UnitName.Equals(
                "kJ", StringComparison.OrdinalIgnoreCase))
            {
                return selected.Amount * 0.239005736m;
            }

            return selected.Amount;
        }

        private static decimal? GetNutrientValue(
            List<FoodNutrientDto> nutrients,
            string nutrientNumber,
            int nutrientId,
            string nutrientName)
        {
            var nutrient = nutrients.FirstOrDefault(n =>
                n.Nutrient != null &&
                (
                    n.Nutrient.Number == nutrientNumber ||
                    n.Nutrient.Id == nutrientId ||
                    n.Nutrient.Name.Contains(
                        nutrientName,
                        StringComparison.OrdinalIgnoreCase)
                ));

            return nutrient?.Amount;
        }

        public Task<Ingredient> GetIngredientEntityAsync(string searchTerm)
        {
            return GetIngredientNutritionAsync(searchTerm);
        }
    }
}
