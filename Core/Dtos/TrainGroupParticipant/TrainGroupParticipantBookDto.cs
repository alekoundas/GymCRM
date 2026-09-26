using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    // One press of Book. The member picked a session on a day, and ticked any of: just
    // that session, and/or every one of the group's weekdays (or days of the month) from
    // that day on. Each ticked option becomes its own row.
    public class TrainGroupParticipantBookDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime SelectedDate { get; set; }

        public bool IsOneOff { get; set; }

        public List<int> RecurringTrainGroupDateIds { get; set; } = new List<int>();

        // Only staff may name somebody else. Left empty, the booking is the caller's.
        public string? UserId { get; set; }

        public int ClientTimezoneOffsetMinutes { get; set; } = 0;
    }
}
