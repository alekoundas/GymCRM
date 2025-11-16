using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantAddDto
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant
        public DateTime? RecurringStartOnDate { get; set; } // Set only for recurring participants.


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupDateId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
