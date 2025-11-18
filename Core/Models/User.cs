using Microsoft.AspNetCore.Identity;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class User : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public override string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public byte[]? ProfileImage { get; set; }


        public int? UserStatusId { get; set; }
        public UserStatus? UserStatus { get; set; }


        public virtual ICollection<Mail> Mails { get; set; } = new Collection<Mail>();
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new Collection<PhoneNumber>();

        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new Collection<WorkoutPlan>();


        public virtual ICollection<UserRole> UserRoles { get; set; } = new Collection<UserRole>();
        public virtual ICollection<TrainGroup> TrainGroups { get; set; } = new Collection<TrainGroup>();
        public virtual ICollection<TrainGroupParticipant> TrainGroupParticipants { get; set; } = new Collection<TrainGroupParticipant>();
        public virtual ICollection<TrainGroupΑttendance> TrainGroupΑttendances { get; set; } = new Collection<TrainGroupΑttendance>();
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
