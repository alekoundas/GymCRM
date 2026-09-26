namespace Core.Models
{
    // Ending a recurring booking, or cancelling a one-off. Nothing is deleted either way.
    public class TrainGroupParticipantRemoveDto
    {
        // Recurring only: the first date the member will no longer attend. Empty means
        // from today.
        public DateTime? FromDate { get; set; }

        public int ClientTimezoneOffsetMinutes { get; set; } = 0;

        // Lifts the 12-hour rule, and only for a caller who is staff as well - a page
        // can say it is an admin page, it cannot make its user one.
        public bool IsAdminPage { get; set; }
    }
}
