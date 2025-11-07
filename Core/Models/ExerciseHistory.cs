namespace Core.Models
{
    public class ExerciseHistory : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sets { get; set; } = string.Empty;
        public int Reps { get; set; }
        public int Weight { get; set; }
        public int GroupNumber { get; set; }
        public int GroupExerciseOrderNumber { get; set; }


        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = null!;
    }
}
