using Core.Enums;
using Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TrainGroupDateDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; } 

        [Required(ErrorMessage = "TrainGroupDateType is required")]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }

        public DateTime? FixedDay { get; set; }
        public DateTime? RecurrenceDayOfWeek { get; set; }
        public DateTime? RecurrenceDayOfMonth { get; set; }


        [Required(ErrorMessage = "TrainGroupId is required")]
        public int TrainGroupId { get; set; }
        //public TrainGroupDto TrainGroup { get; set; } = null!;


        public List<TrainGroupParticipantDto> TrainGroupDateParticipants { get; set; } = new List<TrainGroupParticipantDto>();

        public List<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new List<TrainGroupDateCancellationSubscriber>();
    }
}
