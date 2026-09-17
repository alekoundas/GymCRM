namespace Core.Models
{
    public class TrainGroupΑttendance : BaseModel
    {
        public DateTime AttendanceDate { get; set; }

        // What the session was, copied down when the attendance is recorded. An
        // attendance outlives its group now, and a record of a session nobody can name
        // is no record at all - so everything the calendar shows about it is kept here
        // rather than read back through a row that may be gone or since edited.
        public string TrainGroupTitle { get; set; } = "";
        public string TrainGroupDescription { get; set; } = "";
        public DateTime TrainGroupStartOn { get; set; }
        public DateTime TrainGroupDuration { get; set; }
        public string TrainerFullName { get; set; } = "";

        // Only so the trainer's picture still resolves while that account exists.
        public Guid? TrainerId { get; set; }

        // Null once the group is deleted; the snapshot above carries on.
        public int? TrainGroupId { get; set; }
        public TrainGroup? TrainGroup { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
