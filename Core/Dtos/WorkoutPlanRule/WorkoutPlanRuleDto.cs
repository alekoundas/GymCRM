using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlanRule
{
    public class WorkoutPlanRuleDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;

        // Null falls back to WorkoutPlanRecordingService.AwayGapDays.
        public int? AwayGapDays { get; set; }

        public ICollection<WorkoutPlanRuleWeekDto> Weeks { get; set; } = new Collection<WorkoutPlanRuleWeekDto>();

        // Convenience for the grid - not stored.
        public int WeekCount { get; set; }

        // How many plans point at this rule. Drives the grid's "in use" column.
        public int WorkoutPlanCount { get; set; }

        // True while any plan using this rule has a live recording - edit and delete are blocked.
        public bool IsLocked { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
