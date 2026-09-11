using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Subscription
{
    // An administrator approving or rejecting a pending request.
    public class SubscriptionDecideDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int Id { get; set; }

        public bool IsApproved { get; set; }

        // What is actually granted, which need not be what was asked for.
        public int Amount { get; set; }

        public string AdminComment { get; set; } = string.Empty;

        public bool NotifyUser { get; set; }
    }
}
