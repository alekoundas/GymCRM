using Core.Dtos.Identity;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos
{
    public class TrainGroupDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = "";
        public bool IsRepeating { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        //[Range(typeof(TimeSpan), "00:15:00", "04:00:00", ErrorMessage = "Duration must be between 15 minutes and 4 hours")]
        public TimeSpan Duration { get; set; }

        [Required(ErrorMessage = "MaxParticipants is required")]
        //[Range(1, 50, ErrorMessage = "MaxParticipants must be between 1 and 50")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "TrainerId is required")]
        public Guid TrainerId { get; set; }

        public List<UserDto> RepeatingParticipants { get; set; } = new List<UserDto>();
        public List<TrainGroupDateDto> TrainGroupDates { get; set; } = new List<TrainGroupDateDto>();
    }
}
