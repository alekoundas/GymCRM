namespace Core.Dtos.Subscription
{
    // Everything the two panels on the administrator's home page need, in one call.
    public class SubscriptionChartsDto
    {
        public List<SubscriptionMonthDto> MonthlyApproved { get; set; } = new List<SubscriptionMonthDto>();

        public List<SubscriptionBucketDto> Buckets { get; set; } = new List<SubscriptionBucketDto>();

        // The members furthest into the red, worst first.
        public List<SubscriptionDebtorDto> TopDebtors { get; set; } = new List<SubscriptionDebtorDto>();
    }

    public class SubscriptionMonthDto
    {
        // The first of the month, so the client can format it in its own language.
        public DateTime Month { get; set; }

        public int Amount { get; set; }
    }

    public class SubscriptionBucketDto
    {
        // Which band this is - the client turns it into a label, so no text travels.
        public string Key { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class SubscriptionDebtorDto
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int Balance { get; set; }
    }
}
