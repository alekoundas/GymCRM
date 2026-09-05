using System.Collections.ObjectModel;

namespace Core.Models
{
    public class WorkoutPlanRule : BaseModel
    {
        public string Name { get; set; } = string.Empty;

        // How many days without a recording before the member is asked which week to
        // carry on from. Null means the built-in default, so every rule written before
        // this existed keeps behaving exactly as it did.
        public int? AwayGapDays { get; set; }

        public virtual ICollection<WorkoutPlanRuleWeek> Weeks { get; set; } = new Collection<WorkoutPlanRuleWeek>();
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new Collection<WorkoutPlan>();
    }
}
