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

        public virtual ICollection<Exercise> Exercises { get; set; } = new Collection<Exercise>();
    }
}
