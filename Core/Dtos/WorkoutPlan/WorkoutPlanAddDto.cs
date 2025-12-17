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


        public string Description { get; set; } = string.Empty;

        public bool IsCircular { get; set; }



        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";


        public ICollection<ExerciseAddDto> Exercises { get; set; } = new Collection<ExerciseAddDto>();

    }
}
