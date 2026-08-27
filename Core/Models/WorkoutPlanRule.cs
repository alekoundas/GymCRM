using System.Collections.ObjectModel;

namespace Core.Models
{
    public class WorkoutPlanRule : BaseModel
    {
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<WorkoutPlanRuleWeek> Weeks { get; set; } = new Collection<WorkoutPlanRuleWeek>();
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new Collection<WorkoutPlan>();
    }
}
