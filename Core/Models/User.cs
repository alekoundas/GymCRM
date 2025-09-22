using Microsoft.AspNetCore.Identity;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class User : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public override string Email { get; set; } = "";

        public byte[]? ProfileImage { get; set; }


        public virtual ICollection<Mail> Mails { get; set; } = new Collection<Mail>();
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new Collection<PhoneNumber>();



        // TrainGroup.
        public virtual ICollection<TrainGroup> TrainGroups { get; set; } = new Collection<TrainGroup>();
        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        public virtual ICollection<TrainGroupDateCancellationSubscriber> TrainGroupDatesCancellationSubscriber { get; set; } = new Collection<TrainGroupDateCancellationSubscriber>();
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
