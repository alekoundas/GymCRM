using Core.Dtos.Identity;
using Core.Dtos.TrainGroupDate;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroup
{
    public class TrainGroupDto
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = "";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Duration is required")]
        //[Range(typeof(TimeSpan), "00:15:00", "04:00:00", ErrorMessage = "Duration must be between 5 minutes and 8 hours")]
        public DateTime Duration { get; set; }

        [Required(ErrorMessage = "StartOn is required")]
        //[Range(typeof(TimeSpan), "00:00:00", "23:59:59", ErrorMessage = "StartOn must be a valid time of day")]
        public DateTime StartOn { get; set; }

        [Required(ErrorMessage = "MaxParticipants is required")]
        //[Range(1, 50, ErrorMessage = "MaxParticipants must be between 1 and 50")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "TrainerId is required")]
        public Guid TrainerId { get; set; }

        public ICollection<TrainGroupDateDto> TrainGroupDates { get; set; } = new Collection<TrainGroupDateDto>();
    }
}
