using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TrainGroupDateAddDto
    {
        [Required(ErrorMessage = "TrainGroupDateType is required")]
        public TrainGroupDateTypeEnum TrainGroupDateTypeType { get; set; }

        public DateTime? FixedDay { get; set; }


        public DayOfWeekEnum? RecurrenceDayOfWeek { get; set; }


        [Range(1, 31, ErrorMessage = "RecurrenceDayOfMonth must be between 1 and 31")]
        public int? RecurrenceDayOfMonth { get; set; }
    }
}
