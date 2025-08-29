using Core.Enums;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotRecurrenceDateDto
    {
        public int TrainGroupDateId { get; set; }
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }
        public DateTime Date { get; set; } 

    }
}
