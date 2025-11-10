using Core.Dtos.Exercise;
using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlan
{
    public class WorkoutPlanAddDto
    {

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Title { get; set; } = string.Empty;


        [StringLength(500, ErrorMessage = TranslationKeys._0_cannot_exceed_500_characters)]
        public string Description { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";


        public ICollection<ExerciseAddDto> Exercises { get; set; } = new Collection<ExerciseAddDto>();
    }
}
