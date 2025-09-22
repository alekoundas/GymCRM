using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Dtos.Chart
{
    public class UserGrowthDto
    {
        [JsonPropertyName("date")]
        [Required]
        public DateTime Date { get; set; }

        [JsonPropertyName("cumulative")]
        [Required]
        [Range(0, int.MaxValue)]
        public int Cumulative { get; set; }
    }
}
