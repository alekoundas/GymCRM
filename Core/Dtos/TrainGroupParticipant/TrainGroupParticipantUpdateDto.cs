using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantUpdateDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime SelectedDate { get; set; }

        public int ClientTimezoneOffsetMinutes { get; set; } = 0; 

        public List<TrainGroupParticipantDto> TrainGroupParticipantDtos { get; set; } = new List<TrainGroupParticipantDto>();
    }
}
