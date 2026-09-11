using Core.Dtos.Subscription;
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

        // Null for anybody without the subscriptions claim, so the panels simply are
        // not there rather than arriving empty and looking like a gym with no members.
        public SubscriptionChartsDto? Subscriptions { get; set; }
    }
}
