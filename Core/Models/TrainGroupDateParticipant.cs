namespace Core.Models
{
    public class TrainGroupDateParticipant : BaseModel
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;


        public Guid UserId { get; set; } 
        public User User { get; set; } = null!; 
    }
}
