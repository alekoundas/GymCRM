using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    public class TimeSlotRequestDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime SelectedDate { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
