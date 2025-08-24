using Core.Enums;
using Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TrainGroupDateAddDto
    {
        [Required(ErrorMessage = "RecurringTrainGroupType is required")]
        public RecurringTrainGroupTypeEnum RecurringTrainGroupType { get; set; }

        public DateOnly? FixedDay { get; set; }


        public DayOfWeekEnum? RecurrenceDayOfWeek { get; set; }


        [Range(1, 31, ErrorMessage = "RecurrenceDayOfMonth must be between 1 and 31")]
        public int? RecurrenceDayOfMonth { get; set; }
    }
}
