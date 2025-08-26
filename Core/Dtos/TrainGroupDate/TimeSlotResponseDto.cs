using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotResponseDto
    {
        public int TrainGroupId { get; set; }
        public int TrainGroupDateId { get; set; }
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public bool IsAvailable { get; set; }
    }
}
