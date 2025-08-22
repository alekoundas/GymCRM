using System.Collections.ObjectModel;

namespace Core.Models
{
    public class TrainGroup : BaseModel
    {
        public string Name { get; set; } = "";    
        public string Description { get; set; } = "";    
        public bool IsRepeating { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan StartOn { get; set; }
        public int MaxParticipants { get; set; }


        public Guid TrainerId { get; set; } 
        public User Trainer { get; set; } = null!; 

        public virtual ICollection<User> RepeatingParticipants { get; set; } = new Collection<User>();
        public virtual ICollection<TrainGroupDate> TrainGroupDates { get; set; } = new Collection<TrainGroupDate>();
    }
}
