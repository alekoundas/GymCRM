using Core.Enums;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotRecurrenceDateDto
    {
        public int? TrainGroupDateId { get; set; }
        public int? TrainGroupParticipantId { get; set; } // Used in Profile
        public int? TrainGroupParticipantUnavailableDateId { get; set; } // Used in Profile
        public bool IsOneOff { get; set; }// Used in Profile

        public TrainGroupDateTypeEnum? TrainGroupDateType { get; set; }
        public DateTime Date { get; set; }

        // Which weekday (0 = Sunday) or day of the month a recurring date runs on, as
        // plain numbers. Date above is a made-up day in January 2000 that the json
        // converter moves by the server's offset, so it cannot be read by UTC getters.
        public int? RecurrenceDayOfWeek { get; set; }
        public int? RecurrenceDayOfMonth { get; set; }
        public bool IsUserJoined { get; set; }
        public bool IsUnavailableTrainGroup { get; set; }

        // A session that actually took place, taken from the attendance rather than
        // projected from who is enrolled today. Read only - the date has been and gone.
        public bool IsAttendance { get; set; }
        public int? AttendanceId { get; set; }

    }
}
