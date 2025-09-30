using Core.CustomValidationAttributes;
using Core.Enums;
using Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    [TrainGroupDateTypeValidation]
    public class TrainGroupDateAddDto
    {
        [Required(ErrorMessage = "TrainGroupDateType is required")]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }

        public int? TrainGroupId { get; set; }
        public DateTime? FixedDay { get; set; }
        public DayOfWeek? RecurrenceDayOfWeek { get; set; } // System.DayOfWeek enum
        //[Range(1, 31, ErrorMessage = "MaxParticipants must be between 1 and 50")]
        public int? RecurrenceDayOfMonth { get; set; }// 1-31
        public ICollection<TrainGroupParticipantAddDto> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipantAddDto>();
    }
}
