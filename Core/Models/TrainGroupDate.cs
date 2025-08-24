using Core.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupDate : BaseModel
    {
        public RecurringTrainGroupTypeEnum? RecurringTrainGroupType { get; set; }
        public DateOnly? FixedDay { get; set; }

        public DayOfWeekEnum? RecurrenceDayOfWeek { get; set; }

        [Range(1, 31)]
        public int? RecurrenceDayOfMonth { get; set; }



        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;


        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        public virtual ICollection<TrainGroupCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new Collection<TrainGroupCancellationSubscriber>();
    }
}
