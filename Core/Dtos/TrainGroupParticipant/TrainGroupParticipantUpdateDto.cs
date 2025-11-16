using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantUpdateDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime SelectedDate { get; set; }

        public DateTime? RecurringStartOnDate { get; set; } // Set only for recurring participants.


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";


        public int ClientTimezoneOffsetMinutes { get; set; } = 0; 

        public List<TrainGroupParticipantDto> TrainGroupParticipantDtos { get; set; } = new List<TrainGroupParticipantDto>();
    }
}
