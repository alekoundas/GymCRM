using Core.Dtos.Identity;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantDto
    {
        public int Id { get; set; }

        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupDateId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }
    }
}
