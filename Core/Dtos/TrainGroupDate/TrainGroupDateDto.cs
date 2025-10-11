using Core.CustomValidationAttributes;
using Core.Enums;
using Core.Models;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    [TrainGroupDateTypeValidation]
    public class TrainGroupDateDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int Id { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }


        public DateTime? FixedDay { get; set; }
        public DayOfWeek? RecurrenceDayOfWeek { get; set; } 
        public int? RecurrenceDayOfMonth { get; set; }// 1-31


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }

        public List<TrainGroupParticipantDto> TrainGroupDateParticipants { get; set; } = new List<TrainGroupParticipantDto>();
    }
}
