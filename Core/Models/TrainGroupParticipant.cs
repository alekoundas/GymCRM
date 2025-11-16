using System.Collections.ObjectModel;

namespace Core.Models
{
    public class 
        TrainGroupParticipant : BaseModel
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant
        public DateTime? RecurringStartOnDate { get; set; } // Set only for recurring participants.

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;

        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;


        public Guid UserId { get; set; } 
        public User User { get; set; } = null!;

        public virtual ICollection<TrainGroupParticipantUnavailableDate> TrainGroupParticipantUnavailableDates { get; set; } = new Collection<TrainGroupParticipantUnavailableDate>();

    }
}
