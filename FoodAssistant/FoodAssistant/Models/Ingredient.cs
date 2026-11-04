using System.ComponentModel.DataAnnotations;

namespace FoodAssistant.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // Идентификатор на продукта от USDA API
        public long? FdcId { get; set; }

        // Хранителни стойности за 100 грама
        public decimal CaloriesPer100g { get; set; }

        public decimal ProteinPer100g { get; set; }

        public decimal CarbsPer100g { get; set; }

        public decimal FatPer100g { get; set; }
    }
}
