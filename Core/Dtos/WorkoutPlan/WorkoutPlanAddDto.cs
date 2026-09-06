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

        public int? WorkoutPlanRuleId { get; set; }

        // Model binding drops anything the dto has no property for, so leaving this
        // out meant a plan created with a rule always arrived with no week - silently,
        // because the client had sent one.
        public int? CurrentWeek { get; set; }

    }
}
