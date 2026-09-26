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

        // Kept separately because an attendance carries the trainer's name even when the
        // account or the group behind it is gone.
        public string TrainerFullName { get; set; } = "";
        public int TrainGroupId { get; set; }
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public int SpotsLeft { get; set; }
        public int MaxParticipants { get; set; }
        public bool IsUnavailableTrainGroup { get; set; }
        public int? UnavailableTrainGroupId { get; set; }

        // Booking page: the group date that runs on the requested day, and where the
        // member stands on it - booked (one-off or recurring), or recurring but skipped.
        public int? TrainGroupDateId { get; set; }
        public int? BookedParticipantId { get; set; }
        public bool IsBookedRecurring { get; set; }
        public int? SkippedUnavailableDateId { get; set; }

        public List<TimeSlotRecurrenceDateDto> RecurrenceDates { get; set; } = new List<TimeSlotRecurrenceDateDto>();

    }
}
