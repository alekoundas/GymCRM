using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TrainGroupDateParticipantDto
    {
        public DateTime? SelectedDate { get; set; } // If null repeating subscriber,if not specific date participant

        [Required(ErrorMessage = "TrainGroupDateId is required")]
        public int TrainGroupDateId { get; set; }
        //public TrainGroupDate TrainGroupDate { get; set; } = null!;


        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";
        //public User User { get; set; } = null!; 
    }
}
