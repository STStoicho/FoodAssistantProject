using FoodAssistant.Data;
using FoodAssistant.DTOs.Recipes;
using FoodAssistant.Models;
using FoodAssistant.Services;
using FoodAssistant.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FoodAssistant.Controllers
{
    public class RecipesController : Controller
    {
        private readonly AiRecipeService _aiRecipeService;
        private readonly NutritionService _nutritionService;
        private readonly ApplicationDbContext _dbContext;

        public RecipesController(AiRecipeService aiRecipeService, NutritionService nutritionService, ApplicationDbContext dbContext)
        {
            _aiRecipeService = aiRecipeService;
            _nutritionService = nutritionService;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View(new RecipeGeneratorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(RecipeGeneratorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // 1. Генерираме рецептата чрез AI.
                var recipe = await _aiRecipeService.GenerateRecipeAsync(
                    model.Ingredients,
                    model.TargetCalories,
                    model.Servings
                );

                // 2. Изчисляваме хранителните стойности чрез USDA.
                recipe.Nutrition =
                    await _nutritionService.CalculateNutritionAsync(
                        recipe,
                        model.Servings
                    );

                // 3. Запазваме стойностите за повторно генериране.
                SetRecipeParameters(model);

                // 4. Показваме резултата.
                return View("Result", recipe);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _dbContext.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            var alternatives =
                JsonSerializer.Deserialize<List<HealthyAlternativeDto>>(
                    recipe.HealthyAlternativesJson ?? "[]")
                ?? new List<HealthyAlternativeDto>();

            ViewBag.HealthyAlternatives = alternatives;

            return View(recipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(GeneratedRecipeDto recipe, int servings, int targetCalories)
        {
            if (recipe == null ||
                recipe.Ingredients == null ||
                recipe.Ingredients.Count == 0 ||
                recipe.Nutrition == null)
            {
                TempData["ErrorMessage"] = "Невалидни данни за рецептата.";
                return RedirectToAction(nameof(Index));
            }

            if (servings <= 0)
            {
                TempData["ErrorMessage"] = "Невалиден брой порции.";
                return RedirectToAction(nameof(Index));
            }

            var savedRecipe = new Recipe
            {
                Name = recipe.Name,
                Description = recipe.Description,
                Servings = servings,
                PreparationTimeMinutes = recipe.PreparationTimeMinutes,

                Instructions = string.Join(
                    Environment.NewLine,
                    recipe.Instructions),

                CreatedAtUtc = DateTime.UtcNow,

                TargetCaloriesPerServing = targetCalories,

                CaloriesPerServing = recipe.Nutrition.CaloriesPerServing,
                ProteinPerServing = recipe.Nutrition.ProteinPerServing,
                CarbsPerServing = recipe.Nutrition.CarbsPerServing,
                FatPerServing = recipe.Nutrition.FatPerServing,

                HealthyAlternativesJson = JsonSerializer.Serialize(
                    recipe.HealthyAlternatives)
            };

            foreach (var item in recipe.Ingredients)
            {
                if (item.Quantity <= 0 ||
                    item.Unit != "g" ||
                    string.IsNullOrWhiteSpace(item.UsdaSearchTerm))
                {
                    TempData["ErrorMessage"] = "Невалидно количество или продукт.";
                    return RedirectToAction(nameof(Index));
                }

                var ingredient = await _nutritionService
                    .GetIngredientEntityAsync(item.UsdaSearchTerm);

                savedRecipe.Ingredients.Add(new RecipeIngredient
                {
                    Ingredient = ingredient,
                    IngredientName = item.Name,
                    QuantityGrams = item.Quantity
                });
            }

            _dbContext.Recipes.Add(savedRecipe);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Рецептата беше запазена успешно!";

            return RedirectToAction(nameof(Saved));
        }

        [HttpGet]
        public async Task<IActionResult> Saved()
        {
            var recipes = await _dbContext.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(ri => ri.Ingredient)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();

            return View(recipes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _dbContext.Recipes
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                TempData["ErrorMessage"] = "Рецептата не беше намерена.";
                return RedirectToAction(nameof(Saved));
            }

            _dbContext.Recipes.Remove(recipe);

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Рецептата беше изтрита успешно!";

            return RedirectToAction(nameof(Saved));
        }

        private void SetRecipeParameters(RecipeGeneratorViewModel model)
        {
            SetRecipeParameters(model.Ingredients, model.TargetCalories, model.Servings);
        }

        private void SetRecipeParameters(
            string ingredients,
            int targetCalories,
            int servings)
        {
            ViewBag.Ingredients = ingredients;
            ViewBag.TargetCalories = targetCalories;
            ViewBag.Servings = servings;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplaceIngredient(GeneratedRecipeDto recipe, HealthyAlternativeDto alternative, int servings, string sourceIngredients, int targetCalories)
        {
            try
            {
                if (servings <= 0 ||
                    alternative.QuantityGrams <= 0 ||
                    string.IsNullOrWhiteSpace(alternative.AlternativeName) ||
                    string.IsNullOrWhiteSpace(alternative.UsdaSearchTerm))
                {
                    throw new InvalidOperationException(
                        "Данните за заместващия продукт са невалидни.");
                }

                // Намираме продукта, който трябва да заменим.
                int ingredientIndex = recipe.Ingredients.FindIndex(i =>
                    string.Equals(
                        i.Name,
                        alternative.OriginalIngredientName,
                        StringComparison.OrdinalIgnoreCase));

                if (ingredientIndex == -1)
                {
                    throw new InvalidOperationException(
                        "Продуктът за замяна не беше намерен в рецептата.");
                }

                // Създаваме нов продукт със заместителя.
                var newIngredient = new GeneratedRecipeIngredientDto
                {
                    Name = alternative.AlternativeName,
                    UsdaSearchTerm = alternative.UsdaSearchTerm,
                    Quantity = alternative.QuantityGrams,
                    Unit = "g"
                };

                // Заменяме стария продукт.
                recipe.Ingredients[ingredientIndex] = newIngredient;

                // Премахваме предложенията за вече заменения продукт.
                recipe.HealthyAlternatives.RemoveAll(a =>
                    string.Equals(
                        a.OriginalIngredientName,
                        alternative.OriginalIngredientName,
                        StringComparison.OrdinalIgnoreCase));

                // Преизчисляваме хранителните стойности.
                recipe.Nutrition = await _nutritionService.CalculateNutritionAsync(recipe, servings);

                SetRecipeParameters(sourceIngredients, targetCalories, servings);

                return View("Result", recipe);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;

                return View("Index", new RecipeGeneratorViewModel
                {
                    Ingredients = sourceIngredients ?? string.Empty,
                    TargetCalories = targetCalories,
                    Servings = servings
                });
            }
        }
    }
}
