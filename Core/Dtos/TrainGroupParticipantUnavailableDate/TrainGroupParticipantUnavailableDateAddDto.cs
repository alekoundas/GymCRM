using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupParticipantUnavailableDate
{
    public class TrainGroupParticipantUnavailableDateAddDto
    {

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime UnavailableDate { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupParticipantId { get; set; }


        public int ClientTimezoneOffsetMinutes { get; set; } = 0;

    }
}
