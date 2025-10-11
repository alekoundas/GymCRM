using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Dtos.Chart
{
    public class DailyEmailCountDto
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Count { get; set; }
    }
}
