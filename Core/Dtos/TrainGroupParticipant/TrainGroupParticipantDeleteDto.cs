using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantDeleteDto
    {

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int Id { get; set; }

        public int ClientTimezoneOffsetMinutes { get; set; } = 0;
    }
}
