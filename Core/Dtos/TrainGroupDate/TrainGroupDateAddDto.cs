using Core.CustomValidationAttributes;
using Core.Enums;
using Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupDate
{
    [TrainGroupDateTypeValidation]
    public class TrainGroupDateAddDto
    {
        [Required(ErrorMessage = "TrainGroupDateType is required")]
        public TrainGroupDateTypeEnum TrainGroupDateType { get; set; }

        public int? TrainGroupId { get; set; }
        public DateTime? FixedDay { get; set; }
        public DateTime? RecurrenceDayOfMonth { get; set; }
        public DateTime? RecurrenceDayOfWeek { get; set; }
        public ICollection<TrainGroupParticipantAddDto> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipantAddDto>();
    }
}
