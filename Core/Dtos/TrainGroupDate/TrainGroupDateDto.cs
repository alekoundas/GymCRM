using Core.CustomValidationAttributes;
using Core.Enums;
using Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    [TrainGroupDateTypeValidation]
    public class TrainGroupDateDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; } 

        [Required(ErrorMessage = "TrainGroupDateType is required")]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }


        public DateTime? FixedDay { get; set; }
        public DayOfWeek? RecurrenceDayOfWeek { get; set; } // System.DayOfWeek enum
        //[Range(1, 31, ErrorMessage = "MaxParticipants must be between 1 and 50")]
        public int? RecurrenceDayOfMonth { get; set; }// 1-31


        [Required(ErrorMessage = "TrainGroupId is required")]
        public int TrainGroupId { get; set; }
        //public TrainGroupDto TrainGroup { get; set; } = null!;


        public List<TrainGroupParticipantDto> TrainGroupDateParticipants { get; set; } = new List<TrainGroupParticipantDto>();

        //public List<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new List<TrainGroupDateCancellationSubscriber>();
    }
}
