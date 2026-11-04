using FoodAssistant.Services;
using FoodAssistant.ViewModels;
using Microsoft.AspNetCore.Mvc;
using FoodAssistant.ViewModels;

namespace FoodAssistant.Controllers
{
    public class RecipesController : Controller
    {
        private readonly AiRecipeService _aiRecipeService;

        public RecipesController(AiRecipeService aiRecipeService)
        {
            _aiRecipeService = aiRecipeService;
        }

        public IActionResult Index()
        {
            return View(new RecipeGeneratorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            RecipeGeneratorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var recipe = await _aiRecipeService.GenerateRecipeAsync(
                    model.Ingredients,
                    model.TargetCalories,
                    model.Servings
                );

                return View("Result", recipe);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index", model);
            }
        }
    }
}
