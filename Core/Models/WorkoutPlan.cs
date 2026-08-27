using System.Collections.ObjectModel;

namespace Core.Models
{
    public class WorkoutPlan : BaseModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCircular { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Null until an admin assigns a rule. Start stays disabled while it is null.
        public int? WorkoutPlanRuleId { get; set; }
        public WorkoutPlanRule? WorkoutPlanRule { get; set; }

        // Plain int, not an FK to WorkoutPlanRuleWeek: rules get edited and re-pointed,
        // and a week number stays meaningful when they do.
        public int? CurrentWeek { get; set; }

        public virtual ICollection<Exercise> Exercises { get; set; } = new Collection<Exercise>();

        public virtual ICollection<WorkoutPlanRecording> Recordings { get; set; } = new Collection<WorkoutPlanRecording>();
    }
}
