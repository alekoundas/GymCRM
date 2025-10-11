using Core.CustomValidationAttributes;
using Core.Dtos.TrainGroupDate;
using Core.Models;
using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroup
{
    public class TrainGroupAddDto
    {

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys.Title_cannot_exceed_100_characters)]
        public string Title { get; set; } = "";

        [StringLength(500, ErrorMessage = TranslationKeys.Description_cannot_exceed_500_characters)]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime Duration { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime StartOn { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string? TrainerId { get; set; }

        [MinListCount(1, ErrorMessage = TranslationKeys.At_least_1_Train_Group_Date_is_required)]
        public ICollection<TrainGroupDateAddDto> TrainGroupDates { get; set; } = new Collection<TrainGroupDateAddDto>();
        public ICollection<TrainGroupParticipantAddDto> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipantAddDto>();
    }
}
