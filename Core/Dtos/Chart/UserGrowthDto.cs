using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.Dtos.Chart
{
    public class UserGrowthDto
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Cumulative { get; set; }
    }
}
