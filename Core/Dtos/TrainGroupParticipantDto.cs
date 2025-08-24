using Core.Dtos.Identity;
using Core.Dtos.TrainGroupDate;
using Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos
{
    public class TrainGroupParticipantDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }


        [Required(ErrorMessage = "SelectedDate is required")]
        public DateTime SelectedDate { get; set; }


        [Required(ErrorMessage = "TrainGroupDateId is required")]
        public int TrainGroupDateId { get; set; }
        public TrainGroupDateDto TrainGroupDate { get; set; } = null!;



        [Required(ErrorMessage = "ParticipantId is required")]
        public Guid ParticipantId { get; set; }
        public UserDto Participant { get; set; } = null!;
    }
}
