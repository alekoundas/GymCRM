namespace Core.Models
{
    public class TrainGroupDateCancellationSubscriber : BaseModel
    {
        public DateTime SelectedDate { get; set; } //Specific date of cancelationn

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;


        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
