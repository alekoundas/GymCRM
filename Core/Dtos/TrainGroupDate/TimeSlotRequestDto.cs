using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotRequestDto
    {
        [Required(ErrorMessage = "SelectedDate is required")]
        public DateTime SelectedDate { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";
    }
}
