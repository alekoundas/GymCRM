using System.Collections.ObjectModel;

namespace Core.Models
{
    public class Exercise : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sets { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public int Reps { get; set; }
        public int Weight { get; set; }
        public int GroupNumber { get; set; }
        public int GroupExerciseOrderNumber { get; set; }

        public int WorkoutPlanId { get; set; }
        public WorkoutPlan WorkoutPlan { get; set; } = null!;

        public virtual ICollection<ExerciseHistory> ExerciseHistories { get; set; } = new Collection<ExerciseHistory>();

    }
}
