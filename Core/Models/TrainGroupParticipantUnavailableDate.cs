namespace Core.Models
{
    public class TrainGroupParticipantUnavailableDate : BaseModel
    {
        public DateTime UnavailableDate { get; set; }

        public int TrainGroupParticipantId { get; set; }
        public TrainGroupParticipant TrainGroupParticipant { get; set; } = null!;
    }
}
