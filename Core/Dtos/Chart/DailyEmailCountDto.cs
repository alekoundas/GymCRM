using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Dtos.Chart
{
    public class DailyEmailCountDto
    {
        [JsonPropertyName("date")]
        [Required]
        public DateTime Date { get; set; }

        [JsonPropertyName("count")]
        [Required]
        [Range(0, int.MaxValue)]
        public int Count { get; set; }
    }
}
