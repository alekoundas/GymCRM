using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Chart
{
    public class ChartDataDto
    {
        [Required]
        public List<DailyEmailCountDto> DailyEmails { get; set; } = new List<DailyEmailCountDto>();

        [Required]
        [Range(0, 500)]
        public int AvailableEmails { get; set; }

        [Required]
        public List<UserGrowthDto> UserGrowth { get; set; } = new List<UserGrowthDto>();
    }
}
