using System.Text.Json.Serialization;


namespace FoodAssistant.DTOs.FoodDataCentral
{
    public class FoodSearchResponseDto
    {
        [JsonPropertyName("foods")]
        public List<FoodSearchItemDto> Foods { get; set; } = new();
    }

    public class FoodSearchItemDto
    {
        [JsonPropertyName("fdcId")]
        public long FdcId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
    }
}
