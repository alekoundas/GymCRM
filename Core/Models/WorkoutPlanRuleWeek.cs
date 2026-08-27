namespace Core.Models
{
    public class WorkoutPlanRuleWeek : BaseModel
    {
        // 1-based week number inside the rule.
        public int WeekNumber { get; set; }

        // Shown on the plan while it sits on this week. Blank is allowed.
        public string Message { get; set; } = string.Empty;

        // How many recordings the member may log while the plan sits on this week.
        // Not a calendar week - weeks advance by hand from the Start dialog.
        public int MaxRecordings { get; set; }

        public int WorkoutPlanRuleId { get; set; }
        public WorkoutPlanRule WorkoutPlanRule { get; set; } = null!;
    }
}
