namespace Core.Models
{
    public class TrainGroupParticipant : BaseModel
    {
        public DateTime SelectedDate { get; set; } // Time of train group start

        public int TrainGroupDateId { get; set; }
        public TrainGroupDate TrainGroupDate { get; set; } = null!;


        public Guid ParticipantId { get; set; } 
        public User Participant { get; set; } = null!; 
    }
}
