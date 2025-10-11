using Core.CustomValidationAttributes;
using Core.Enums;
using Core.Models;
using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    [TrainGroupDateTypeValidation]
    public class TrainGroupDateAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }

        public int? TrainGroupId { get; set; }
        public DateTime? FixedDay { get; set; }
        public DayOfWeek? RecurrenceDayOfWeek { get; set; }

        public int? RecurrenceDayOfMonth { get; set; }// 1-31
        public ICollection<TrainGroupParticipantAddDto> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipantAddDto>();
    }
}
