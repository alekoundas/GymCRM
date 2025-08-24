using Core.Enums;
using Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos
{
    public class TrainGroupDateDto
    {
        [Required(ErrorMessage = "Id is required")]
        public string Id { get; set; } = "";

        public RecurringTrainGroupTypeEnum? RepeatingTrainGroupType { get; set; }

        public DateOnly? FixedDay { get; set; }


        public DayOfWeekEnum? RecurrenceDayOfWeek { get; set; }


        [Range(1, 31, ErrorMessage = "RecurrenceDayOfMonth must be between 1 and 31")]
        public int? RecurrenceDayOfMonth { get; set; }


        [Required(ErrorMessage = "TrainGroupId is required")]
        public int TrainGroupId { get; set; }
        public TrainGroupDto TrainGroup { get; set; } = null!;


        public List<TrainGroupParticipantDto> TrainGroupParticipants { get; set; } = new List<TrainGroupParticipantDto>();

        public List<TrainGroupCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new List<TrainGroupCancellationSubscriber>();
    }
}
