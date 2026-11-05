using System.Text.Json.Serialization;

namespace FoodAssistant.DTOs.FoodDataCentral
{
    public class FoodDetailsResponseDto
    {
        [JsonPropertyName("fdcId")]
        public long FdcId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("foodNutrients")]
        public List<FoodNutrientDto> FoodNutrients { get; set; } = new();
    }

    public class FoodNutrientDto
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("nutrient")]
        public NutrientInfoDto? Nutrient { get; set; }
    }

    public class NutrientInfoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("unitName")]
        public string UnitName { get; set; } = string.Empty;
    }
}
