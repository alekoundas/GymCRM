using Core.Dtos.User;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotResponseDto
    {
        public int Id { get; set; } 
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Guid TrainerId { get; set; }
        public UserDto Trainer { get; set; } = null!;
        public int TrainGroupId { get; set; }
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public int SpotsLeft { get; set; }
        public bool IsUnavailableTrainGroup { get; set; }
        public int? UnavailableTrainGroupId { get; set; }

        public List<TimeSlotRecurrenceDateDto> RecurrenceDates { get; set; } = new List<TimeSlotRecurrenceDateDto>();

    }
}
