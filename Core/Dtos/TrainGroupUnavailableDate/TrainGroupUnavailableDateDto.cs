using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupParticipantUnavailableDate
{
    public class TrainGroupUnavailableDateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "UnavailableDate is required")]
        public DateTime UnavailableDate { get; set; }

        [Required(ErrorMessage = "TrainGroupParticipantId is required")]
        public int TrainGroupId { get; set; }
    }
}
