using System.Text.Json;
using FoodAssistant.DTOs.FoodDataCentral;

namespace FoodAssistant.Services
{
    public class FoodDataCentralService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FoodDataCentralService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // Търсене на продукти по име
        public async Task<List<FoodSearchItemDto>> SearchFoodsAsync(
            string searchTerm)
        {
            string apiKey = _configuration["USDA:ApiKey"] ?? throw new InvalidOperationException("Липсва USDA API ключ.");

            string encodedSearchTerm = Uri.EscapeDataString(searchTerm);

            string url = $"foods/search?query={encodedSearchTerm}" + $"&pageSize=10&api_key={apiKey}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<FoodSearchResponseDto>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result?.Foods ?? new List<FoodSearchItemDto>();
        }

        // Извличане на хранителните стойности на продукт
        public async Task<FoodDetailsResponseDto?> GetFoodDetailsAsync(long fdcId)
        {
            string apiKey = _configuration["USDA:ApiKey"] ?? throw new InvalidOperationException("Липсва USDA API ключ.");

            string url = $"food/{fdcId}?api_key={apiKey}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<FoodDetailsResponseDto>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }
}
