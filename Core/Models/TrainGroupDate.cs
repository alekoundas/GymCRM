using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class TrainGroupDate : BaseModel
    {
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }
        
        public DateTime? FixedDay { get; set; }
        public DayOfWeek? RecurrenceDayOfWeek { get; set; } // System.DayOfWeek enum
        public int? RecurrenceDayOfMonth { get; set; }// 1-31


        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;


        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        //public virtual ICollection<TrainGroupDateCancellationSubscriber> TrainGroupDateCancellationSubscribers { get; set; } = new Collection<TrainGroupDateCancellationSubscriber>();
    }
}
