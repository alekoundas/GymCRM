using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Subscription
{
    // What an administrator fills in to grant credits directly.
    public class SubscriptionAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int Amount { get; set; }

        public string AdminComment { get; set; } = string.Empty;

        public bool NotifyUser { get; set; }
    }
}
