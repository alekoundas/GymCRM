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



        public string Description { get; set; } = string.Empty;

        public bool IsCircular { get; set; }



        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }



        public ICollection<ExerciseDto> Exercises { get; set; } = new Collection<ExerciseDto>();


        // Null until an admin assigns one. Start stays disabled while null.
        public int? WorkoutPlanRuleId { get; set; }
        public string WorkoutPlanRuleName { get; set; } = string.Empty;

        // Plain week number, not an FK - see WorkoutPlan.CurrentWeek.
        public int? CurrentWeek { get; set; }

        // Weeks in the assigned rule. Zero when there is no rule.
        public int WeekCount { get; set; }

        // The rule's message for CurrentWeek. Empty when blank, orphaned, or ruleless.
        public string CurrentWeekMessage { get; set; } = string.Empty;

        // Derived from the recordings - drives the grid's status column.
        public bool IsRunning { get; set; }
        public int? ElapsedSeconds { get; set; }
        public bool HasIncompleteRecording { get; set; }

        // Drives the grid's default sort.
        public DateTime? LastRecordingOn { get; set; }

        public int RecordingCount { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}