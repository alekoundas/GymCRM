using Core.Enums;

namespace Core.Models
{
    public class Subscription : BaseModel
    {
        public int Amount { get; set; }
        public int? RequestedAmount { get; set; }
        public string AdminComment { get; set; } = string.Empty;
        public string MemberComment { get; set; } = string.Empty;

        public DateTime? DecidedOn { get; set; }
        public SubscriptionStatusEnum Status { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
