using FoodAssistant.DTOs.Recipes;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FoodAssistant.Services
{
    public class AiRecipeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiRecipeService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<GeneratedRecipeDto> GenerateRecipeAsync(
            string ingredients,
            int targetCalories,
            int servings)
        {
            string apiKey = _configuration["Groq:ApiKey"]
                ?? throw new InvalidOperationException(
                    "Липсва Groq API ключ.");

            string prompt = $"""
    Създай здравословна и реалистична рецепта
    на български език.

    Налични продукти: {ingredients}

    Желани калории на порция: {targetCalories} kcal.
    Брой порции: {servings}.

    ИЗИСКВАНИЯ КЪМ РЕЦЕПТАТА:

    1. Използвай предимно наличните продукти.
    2. Количествата трябва да са за цялата рецепта.
    3. Всички количества трябва да бъдат в грамове.
    4. Времето за приготвяне трябва да е цяло число.
    5. Инструкциите трябва да са ясни и последователни.
    6. Съобрази общото количество на продуктите с броя порции.
    7. Стреми се към зададените калории за една порция.

    ИЗИСКВАНИЯ КЪМ ПРОДУКТИТЕ:

    За всеки продукт посочи:
    - name: име на български език.
    - usdaSearchTerm: точно английско наименование
      за търсене в USDA FoodData Central.
    - quantity: количество в грамове за цялата рецепта.
    - unit: винаги "g".

    ИЗИСКВАНИЯ КЪМ АЛТЕРНАТИВИТЕ:

    Предложи до 3 здравословни алтернативи.

    За всяка алтернатива посочи:
    - originalIngredientName: точното име на продукта
      от рецептата, който се заменя.
    - alternativeName: име на заместителя на български.
    - usdaSearchTerm: английско име за търсене в USDA.
    - quantityGrams: количество в грамове за цялата рецепта.
    - reason: защо замяната е подходяща.

    Алтернативите трябва да заменят реални продукти
    от рецептата.

    ПОЛЕТО healthyAlternatives Е ЗАДЪЛЖИТЕЛНО!

    Винаги включвай healthyAlternatives в JSON обекта.
    Ако няма подходящи алтернативи, върни:
    "healthyAlternatives": []

    Никога не пропускай това поле.

    ХРАНИТЕЛНИ СТОЙНОСТИ:

    Не измисляй калории, протеини, въглехидрати
    или мазнини. Те ще бъдат изчислени от приложението
    чрез USDA FoodData Central.

    ВАЖНО:

    Върни само един валиден JSON обект.
    JSON обектът трябва да съдържа всички задължителни
    полета от зададената схема.

    Никога не пропускай поле, дори когато стойността му
    е празен списък.

    Винаги включвай:
    "healthyAlternatives": []

    ако няма алтернативи.

    Не добавяй Markdown, коментари или допълнителни полета.
    """;

            var requestBody = new
            {
                model = "openai/gpt-oss-20b",
                max_completion_tokens = 4096,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Ти си професионален асистент по здравословно хранене. Отговаряй на български."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },

                response_format = new
                {
                    type = "json_schema",

                    json_schema = new
                    {
                        name = "generated_recipe",
                        strict = true,
                        schema = new
                        {
                            type = "object",

                            properties = new
                            {
                                name = new { type = "string" },

                                description = new { type = "string" },

                                preparationTimeMinutes =
                                    new { type = "integer" },

                                ingredients = new
                                {
                                    type = "array",

                                    items = new
                                    {
                                        type = "object",

                                        properties = new
                                        {
                                            name = new { type = "string" },

                                            usdaSearchTerm =
                                                new { type = "string" },

                                            quantity = new { type = "number" },

                                            unit = new { type = "string" }
                                        },

                                        required = new[]
                                        {
                                            "name",
                                            "usdaSearchTerm",
                                            "quantity",
                                            "unit"
                                        },

                                        additionalProperties = false
                                    }
                                },

                                instructions = new
                                {
                                    type = "array",
                                    items = new { type = "string" }
                                },

                                healthyAlternatives = new
                                {
                                    type = "array",

                                    items = new
                                    {
                                        type = "object",

                                        properties = new
                                        {
                                            originalIngredientName =
                                                new { type = "string" },

                                            alternativeName =
                                                new { type = "string" },

                                            usdaSearchTerm =
                                                new { type = "string" },

                                            quantityGrams =
                                                new { type = "number" },

                                            reason =
                                                new { type = "string" }
                                        },

                                        required = new[]
                                        {
                                            "originalIngredientName",
                                            "alternativeName",
                                            "usdaSearchTerm",
                                            "quantityGrams",
                                            "reason"
                                        },

                                        additionalProperties = false
                                    }
                                }
                            },

                            required = new[]
                            {
                                "name",
                                "description",
                                "preparationTimeMinutes",
                                "ingredients",
                                "instructions",
                                "healthyAlternatives"
                            },

                            additionalProperties = false
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request);

            string responseJson =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Groq API грешка ({(int)response.StatusCode}): {responseJson}");
            }

            using JsonDocument document =
                JsonDocument.Parse(responseJson);

            var root = document.RootElement;

            var choices = root.GetProperty("choices");

            if (choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Groq не върна резултат.");
            }

            var message = choices[0].GetProperty("message");

            if (message.TryGetProperty("refusal", out var refusal))
            {
                throw new InvalidOperationException(
                    $"AI отказа генерирането: {refusal.GetString()}");
            }

            string? recipeJson = message
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(recipeJson))
            {
                throw new InvalidOperationException(
                    "Получен е празен отговор от Groq.");
            }

            try
            {
                var recipe = JsonSerializer.Deserialize<GeneratedRecipeDto>(
                    recipeJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (recipe == null ||
                    string.IsNullOrWhiteSpace(recipe.Name) ||
                    recipe.Ingredients == null ||
                    recipe.Ingredients.Count == 0 ||
                    recipe.Instructions == null ||
                    recipe.Instructions.Count == 0)
                {
                    throw new InvalidOperationException(
                        "AI върна непълна рецепта.");
                }

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (string.IsNullOrWhiteSpace(ingredient.Name) ||
                        string.IsNullOrWhiteSpace(ingredient.UsdaSearchTerm) ||
                        ingredient.Quantity <= 0 ||
                        ingredient.Unit != "g")
                    {
                        throw new InvalidOperationException(
                            "AI върна невалиден продукт в рецептата.");
                    }
                }

                recipe.HealthyAlternatives ??= new();

                return recipe;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "AI върна отговор в неправилен JSON формат.", ex);
            }
        }
    }
}
