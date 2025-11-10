using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.ExerciseHistory
{
    public class ExerciseHistoryDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;


        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Sets { get; set; }= string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Reps { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Weight { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;



        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int GroupNumber { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int GroupExerciseOrderNumber { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int ExerciseId { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
