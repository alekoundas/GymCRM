using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Exercise
{
    public class ExerciseDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;


        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [Range(1, int.MaxValue, ErrorMessage = TranslationKeys._0_must_be_a_positive_number)]
        public int Sets { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [Range(1, int.MaxValue, ErrorMessage = TranslationKeys._0_must_be_a_positive_number)]
        public int Reps { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [Range(1, int.MaxValue, ErrorMessage = TranslationKeys._0_must_be_a_positive_number)]
        public int Weight { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int WorkoutPlanId { get; set; }
    }
}
