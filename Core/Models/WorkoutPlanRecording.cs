namespace Core.Models
{
    public class WorkoutPlanRecording : BaseModel
    {
        public DateTime StartedOn { get; set; }

        // Null while running, and stays null when the recording lapses past the cut-off.
        public DateTime? CompletedOn { get; set; }

        // Stored rather than computed so the grid can sort on it.
        public int? DurationSeconds { get; set; }

        // Snapshot of the plan's week at the time - never re-derived, so history stays true.
        public int WeekNumber { get; set; }

        // Nullable so deleting a plan keeps the history.
        public int? WorkoutPlanId { get; set; }
        public WorkoutPlan? WorkoutPlan { get; set; }

        // Snapshot, so a deleted or retitled plan still reads correctly.
        public string WorkoutPlanTitle { get; set; } = string.Empty;

        // The member the plan belongs to - not whoever wrote the row, which is CreatedBy_Id.
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
