using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlanRule
{
    public class WorkoutPlanRuleAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;

        public ICollection<WorkoutPlanRuleWeekAddDto> Weeks { get; set; } = new Collection<WorkoutPlanRuleWeekAddDto>();
    }
}
