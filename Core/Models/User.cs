using Microsoft.AspNetCore.Identity;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class User : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public virtual ICollection<ContactInformation> ContactInformations { get; set; } = new Collection<ContactInformation>();

        // TrainGroup.
        public virtual ICollection<TrainGroup> Trainers { get; set; } = new Collection<TrainGroup>();
        public virtual ICollection<TrainGroup> RepeatingTrainGroups { get; set; } = new Collection<TrainGroup>();
        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        public virtual ICollection<TrainGroupCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; } = new Collection<TrainGroupCancellationSubscriber>();

    }
}
