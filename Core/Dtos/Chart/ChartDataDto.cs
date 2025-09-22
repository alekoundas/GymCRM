using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Dtos.Chart
{
    public class ChartDataDto
    {
        [JsonPropertyName("dailyEmails")]
        [Required]
        public List<DailyEmailCountDto> DailyEmails { get; set; } = new List<DailyEmailCountDto>();

        [JsonPropertyName("availableEmails")]
        [Required]
        [Range(0, 500)]
        public int AvailableEmails { get; set; }

        [JsonPropertyName("userGrowth")]
        [Required]
        public List<UserGrowthDto> UserGrowth { get; set; } = new List<UserGrowthDto>();
    }
}
