using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlanRule
{
    public class WorkoutPlanRuleWeekDto
    {
        public int? Id { get; set; }

        [Range(1, 15, ErrorMessage = TranslationKeys._0_is_required)]
        public int WeekNumber { get; set; }

        public string Message { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = TranslationKeys._0_is_required)]
        public int MaxRecordings { get; set; } = 1;

        public int WorkoutPlanRuleId { get; set; }
    }
}
