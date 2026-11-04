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
                Създай здравословна рецепта на български език.

                Налични продукти: {ingredients}

                Желани калории на една порция: {targetCalories} kcal.
                Брой порции: {servings}.

                Използвай предимно посочените продукти.
                Посочи реалистични количества за всички порции.
                Използвай мерни единици g, ml или бр.

                Опиши стъпките за приготвяне последователно.
                Предложи няколко здравословни алтернативи.

                Не измисляй точни хранителни стойности.
                Те ще бъдат изчислени отделно от приложението.
                """;

            var requestBody = new
            {
                model = "openai/gpt-oss-20b",

                instructions =
                    "Ти си AI асистент за здравословно хранене. Отговаряй на български.",

                input = prompt,

                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "generated_recipe",

                        schema = new
                        {
                            type = "object",

                            properties = new
                            {
                                name = new { type = "string" },
                                description = new { type = "string" },
                                preparationTimeMinutes = new { type = "integer" },

                                ingredients = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            name = new { type = "string" },
                                            quantity = new { type = "number" },
                                            unit = new { type = "string" }
                                        },
                                        required = new[]
                                        {
                                            "name",
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
                                    items = new { type = "string" }
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
                "https://api.groq.com/openai/v1/responses");

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

            if (root.TryGetProperty("status", out var status) &&
                status.GetString() != "completed")
            {
                throw new InvalidOperationException(
                    "Groq не завърши генерирането на рецептата.");
            }

            string? recipeJson = null;

            foreach (var outputItem in root.GetProperty("output").EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var content))
                    continue;

                foreach (var contentItem in content.EnumerateArray())
                {
                    string? type = contentItem
                        .GetProperty("type")
                        .GetString();

                    if (type == "refusal")
                    {
                        throw new InvalidOperationException(
                            "AI не успя да генерира рецептата.");
                    }

                    if (type == "output_text")
                    {
                        recipeJson = contentItem
                            .GetProperty("text")
                            .GetString();

                        break;
                    }
                }

                if (recipeJson != null)
                    break;
            }

            if (string.IsNullOrWhiteSpace(recipeJson))
            {
                throw new InvalidOperationException(
                    "Не беше получена рецепта от Groq.");
            }

            return JsonSerializer.Deserialize<GeneratedRecipeDto>(
                recipeJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
                ?? throw new InvalidOperationException(
                    "Рецептата не може да бъде обработена.");
        }
    }
}
