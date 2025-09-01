using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantUpdateDto
    {
        [Required(ErrorMessage = "TrainGroupId is required")]
        public int TrainGroupId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "SelectedDate is required")]
        public DateTime SelectedDate { get; set; } 

        public List<TrainGroupParticipantDto> TrainGroupParticipantDtos { get; set; } = new List<TrainGroupParticipantDto>();
    }
}
