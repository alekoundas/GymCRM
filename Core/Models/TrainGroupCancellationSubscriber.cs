namespace Core.Models
{
    public class TrainGroupCancellationSubscriber : BaseModel
    {
        public DateTime SelectedDate { get; set; } // Time of train group start

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;


        public Guid CancellationSubscriberId { get; set; }
        public User CancellationSubscriber { get; set; } = null!;
    }
}
