namespace Core.Models
{
    public class TrainGroupΑttendance : BaseModel
    {
        public DateTime AttendanceDate { get; set; }

        public int TrainGroupId { get; set; }
        public TrainGroup TrainGroup { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
