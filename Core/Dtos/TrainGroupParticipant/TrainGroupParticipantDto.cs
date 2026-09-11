using Core.Dtos.User;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantDto
    {
        public int Id { get; set; }

        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant
        public DateTime? RecurringStartOnDate { get; set; } // Set only for recurring participants.

        public bool HasAttendance { get; set; }

        // Lessons still owed to this member, so whoever is taking attendance can see
        // who has run out before marking them present.
        public int SubscriptionBalance { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupDateId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }

    }
}
