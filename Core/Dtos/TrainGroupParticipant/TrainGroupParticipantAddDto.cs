using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupParticipantAddDto
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant

        public int? TrainGroupDateId { get; set; }

        [Required(ErrorMessage = "TrainGroupId is required")]
        public int TrainGroupId { get; set; }


        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";
    }
}
