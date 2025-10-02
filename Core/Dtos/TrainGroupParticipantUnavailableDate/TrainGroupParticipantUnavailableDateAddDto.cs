using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupParticipantUnavailableDate
{
    public class TrainGroupParticipantUnavailableDateAddDto
    {

        [Required(ErrorMessage = "UnavailableDate is required")]
        public DateTime UnavailableDate { get; set; }

        [Required(ErrorMessage = "TrainGroupParticipantId is required")]
        public int TrainGroupParticipantId { get; set; }
    }
}
