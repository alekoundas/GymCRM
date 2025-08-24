using Core.Dtos.TrainGroupDate;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos
{
    public class TrainGroupCancellationSubscriberDto
    {
        [Required(ErrorMessage = "Id is required")]
        public string Id { get; set; } = "";


        [Required(ErrorMessage = "SelectedDate is required")]
        public DateTime SelectedDate { get; set; }


        [Required(ErrorMessage = "TrainGroupDateId is required")]
        public int TrainGroupDateId { get; set; }
        public TrainGroupDateDto TrainGroupDate { get; set; } = null!;


        [Required(ErrorMessage = "CancellationSubscriberId is required")]
        public Guid CancellationSubscriberId { get; set; }
    }
}
