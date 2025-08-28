using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotResponseDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Guid TrainerId { get; set; }
        public int TrainGroupId { get; set; }
        public int TrainGroupDateId { get; set; }
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public int SpotsLeft { get; set; }
    }
}
