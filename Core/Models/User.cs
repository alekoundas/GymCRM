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
        public virtual ICollection<TrainGroupDateParticipant> TrainGroupDatesParticipant { get; set; } = new Collection<TrainGroupDateParticipant>();
        public virtual ICollection<TrainGroupDateCancellationSubscriber> TrainGroupDatesCancellationSubscriber { get; set; } = new Collection<TrainGroupDateCancellationSubscriber>();

    }
}
