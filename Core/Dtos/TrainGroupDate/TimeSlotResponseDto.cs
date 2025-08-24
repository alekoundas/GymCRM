using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotResponseDto
    {
        public int TrainGroupDateId { get; set; }
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public string DisplayDate { get; set; } = "";
        public bool IsAvailable { get; set; }
    }
}
