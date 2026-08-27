using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.WorkoutPlanRecording
{
    public class WorkoutPlanStartRequestDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int WorkoutPlanId { get; set; }

        // The week the member confirmed in the dialog. The server validates it against
        // the rule rather than trusting it, and stores it on the plan and the recording.
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int WeekNumber { get; set; }
    }
}
