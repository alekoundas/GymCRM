using System.Collections.ObjectModel;

namespace Core.Models
{
    public class TrainGroup : BaseModel
    {
        public string Title { get; set; } = "";    
        public string Description { get; set; } = "";    
        public DateTime Duration { get; set; }
        public DateTime StartOn { get; set; }
        public int MaxParticipants { get; set; }


        public Guid TrainerId { get; set; } 
        public User Trainer { get; set; } = null!; 

        public virtual ICollection<User> RepeatingParticipants { get; set; } = new Collection<User>();
        public virtual ICollection<TrainGroupDate> TrainGroupDates { get; set; } = new Collection<TrainGroupDate>();
    }
}
