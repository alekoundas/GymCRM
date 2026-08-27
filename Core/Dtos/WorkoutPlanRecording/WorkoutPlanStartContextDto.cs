using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Dtos.WorkoutPlanRecording
{
    // Everything the client needs to decide which Start dialog to show,
    // so the decision is made from one round trip and one source of truth.
    public class WorkoutPlanStartContextDto
    {
        public int WorkoutPlanId { get; set; }

        public WorkoutPlanStartScenarioEnum Scenario { get; set; }

        public bool HasRule { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public int WeekCount { get; set; }

        public int? CurrentWeek { get; set; }
        public string CurrentWeekMessage { get; set; } = string.Empty;

        // Where "advance" lands. Wraps to 1 past the last week, and is 1 when orphaned.
        public int NextWeek { get; set; }

        // Consecutive recordings on CurrentWeek, per the backward walk.
        public int RecordingsOnCurrentWeek { get; set; }

        // Limit for CurrentWeek. Zero when the week is orphaned.
        public int MaxRecordings { get; set; }

        public int RemainingOnCurrentWeek { get; set; }

        public DateTime? LastRecordingOn { get; set; }
        public int? DaysSinceLastRecording { get; set; }

        // Weeks the "away too long" dialog may offer - always 1..WeekCount.
        public ICollection<int> AvailableWeeks { get; set; } = new Collection<int>();

        // Set only when a recording is live for this plan.
        public WorkoutPlanRecordingDto? RunningRecording { get; set; }
    }
}
