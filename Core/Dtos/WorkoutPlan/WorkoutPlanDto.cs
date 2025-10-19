using Core.Dtos.Exercise;
using Core.Dtos.User;
using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlan
{
    public class WorkoutPlanDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Title { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }



        public ICollection<ExerciseDto> Exercises { get; set; } = new Collection<ExerciseDto>();
    }
}