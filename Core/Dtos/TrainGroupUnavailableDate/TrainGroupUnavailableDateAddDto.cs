using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupUnavailableDate
{
    public class TrainGroupUnavailableDateAddDto
    {

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime UnavailableDate { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }
    }
}
