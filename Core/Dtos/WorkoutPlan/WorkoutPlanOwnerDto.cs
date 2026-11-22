using Core.Dtos.UserStatus;

namespace Core.Dtos.WorkoutPlan
{
    // Who a plan belongs to, as the plan screens show it: name, username, photo and
    // status colour. Deliberately not UserDto - a plan goes to whoever may see it, and
    // the member's email, address and medical history have no place there. No
    // validation either: it is never saved through a plan (the mapping ignores it).
    public class WorkoutPlanOwnerDto
    {
        public string? Id { get; set; }
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public byte[]? ProfileImage { get; set; }
        public int? UserStatusId { get; set; }
        public UserStatusDto? UserStatus { get; set; }
    }
}
