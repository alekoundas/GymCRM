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
        public bool IsUserJoined { get; set; }
        public bool IsUnavailableTrainGroup { get; set; }

        // A session that actually took place, taken from the attendance rather than
        // projected from who is enrolled today. Read only - the date has been and gone.
        public bool IsAttendance { get; set; }
        public int? AttendanceId { get; set; }

    }
}
