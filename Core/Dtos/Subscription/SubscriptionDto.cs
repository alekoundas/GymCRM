using Core.Dtos.User;
using Core.Enums;

namespace Core.Dtos.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }

        public int? RequestedAmount { get; set; }
        public int Amount { get; set; }
        public SubscriptionStatusEnum Status { get; set; }
        public string MemberComment { get; set; } = string.Empty;
        public string AdminComment { get; set; } = string.Empty;
        public DateTime? DecidedOn { get; set; }

        public string UserId { get; set; } = "";
        public UserDto? User { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
