using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Exercise
{
    public class ExerciseDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;


        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string Sets { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string Reps { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string Weight { get; set; } = string.Empty;


        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string VideoUrl { get; set; } = string.Empty;



        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int GroupNumber { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int GroupExerciseOrderNumber { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int WorkoutPlanId { get; set; }

        // Filled for the admin's exercise history page: whose plan it is, and which.
        public string WorkoutPlanTitle { get; set; } = string.Empty;
        public bool IsWorkoutPlanInactive { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Core.Dtos.User.UserDto? User { get; set; }
    }
}
