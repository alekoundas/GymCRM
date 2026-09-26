using Core.Enums;

namespace Core.Models
{
    public class TrainGroupParticipantBookingsRequestDto
    {
        // Only staff may ask after somebody else. Left empty, it is the caller.
        public string? UserId { get; set; }

        public int ClientTimezoneOffsetMinutes { get; set; } = 0;
    }

    // One of a member's bookings as the booking page lists it: running, coming up,
    // or ended and kept.
    public class TrainGroupParticipantBookingDto
    {
        public int Id { get; set; }
        public int TrainGroupId { get; set; }
        public int TrainGroupDateId { get; set; }
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }

        public string Title { get; set; } = "";
        public string TrainerFullName { get; set; } = "";
        public DateTime StartOn { get; set; }
        public DateTime Duration { get; set; }

        // A one-off, or a fixed-day group: one date, in Date.
        public bool IsOneOff { get; set; }
        public DateTime? Date { get; set; }

        // Recurring: which day, and the stretch it covers. EndOnDate is the first date
        // no longer covered.
        public int? RecurrenceDayOfWeek { get; set; }
        public int? RecurrenceDayOfMonth { get; set; }
        public DateTime? StartOnDate { get; set; }
        public DateTime? EndOnDate { get; set; }

        public DateTime? RemovedOn { get; set; }
        public string RemovedBy_FullName { get; set; } = "";

        public DateTime? NextSessionDate { get; set; }
        public DateTime? LastSessionDate { get; set; }

        public List<TrainGroupParticipantSkipDto> UpcomingSkips { get; set; } = new List<TrainGroupParticipantSkipDto>();
    }

    public class TrainGroupParticipantSkipDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }
}
