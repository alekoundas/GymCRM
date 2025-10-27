namespace Core.Models
{
    public class TrainGroupUnavailableDate : BaseModel
    {
        public DateTime UnavailableDate { get; set; }

        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;
    }
}
