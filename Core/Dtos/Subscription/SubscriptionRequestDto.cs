using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Subscription
{
    // What a member sends when asking. There is deliberately no amount that counts
    // and no status here - the endpoint decides both, so a crafted payload cannot
    // grant anybody anything.
    public class SubscriptionRequestDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int RequestedAmount { get; set; }

        public string MemberComment { get; set; } = string.Empty;
    }
}
