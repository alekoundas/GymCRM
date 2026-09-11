using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Subscription
{
    // Removing an entry moves somebody's balance, so the administrator is asked
    // whether the member should hear about it.
    public class SubscriptionRemoveDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int Id { get; set; }

        public bool NotifyUser { get; set; }
    }
}
