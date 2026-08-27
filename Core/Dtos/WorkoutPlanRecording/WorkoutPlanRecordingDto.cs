using Core.Dtos.User;

namespace Core.Dtos.WorkoutPlanRecording
{
    public class WorkoutPlanRecordingDto
    {
        public int? Id { get; set; }

        public DateTime StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public int? DurationSeconds { get; set; }
        public int WeekNumber { get; set; }

        public int? WorkoutPlanId { get; set; }
        public string WorkoutPlanTitle { get; set; } = string.Empty;

        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }

        // Derived: still open and inside the cut-off.
        public bool IsRunning { get; set; }

        // Derived: open but past the cut-off - it can never be stopped or given a duration.
        public bool IsIncomplete { get; set; }

        // Computed on the server so the running timer never depends on the client
        // parsing a timestamp. Only set while IsRunning.
        public int? ElapsedSeconds { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
