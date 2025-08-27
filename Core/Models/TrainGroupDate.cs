using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class TrainGroupDate : BaseModel
    {
        public TrainGroupDateTypeEnum? TrainGroupDateType { get; set; }
        
        public DateTime? FixedDay { get; set; }
        public DateTime? RecurrenceDayOfWeek { get; set; }
        public DateTime? RecurrenceDayOfMonth { get; set; }


        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;


        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        public virtual ICollection<TrainGroupCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new Collection<TrainGroupCancellationSubscriber>();
    }
}
