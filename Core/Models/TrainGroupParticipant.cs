using System.Collections.ObjectModel;

namespace Core.Models
{
    public class
        TrainGroupParticipant : BaseModel
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant
        public DateTime? RecurringStartOnDate { get; set; } // Set only for recurring participants.

        // The first date a recurring booking no longer covers. Null while it runs on.
        public DateTime? RecurringEndOnDate { get; set; }

        // When the booking was ended or cancelled, and by whom. A booking is never
        // deleted any more: the profile calendar shows every session between booking
        // and leaving, attended or missed, and that needs the row to still be here.
        public DateTime? RemovedOn { get; set; }
        public string RemovedBy_Id { get; set; } = string.Empty;
        public string RemovedBy_FullName { get; set; } = string.Empty;

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;

        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;


        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public virtual ICollection<TrainGroupParticipantUnavailableDate> TrainGroupParticipantUnavailableDates { get; set; } = new Collection<TrainGroupParticipantUnavailableDate>();

    }
}
