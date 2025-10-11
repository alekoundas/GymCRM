using Core.Translations;
using Microsoft.Extensions.Localization;  // For IStringLocalizer
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Core.CustomValidationAttributes
{
    public class MinListCountAttribute : ValidationAttribute
    {
        private readonly int _minCount;

        public MinListCountAttribute(int minCount)
        {
            _minCount = minCount;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IList list)
            {
                if (list.Count < _minCount)
                {
                    // Get the IStringLocalizer from the validation context (returns object)
                    var localizerObj = validationContext.GetService(typeof(IStringLocalizer));

                    // Cast to IStringLocalizer (non-generic, matches your setup)
                    if (localizerObj is IStringLocalizer localizer)
                    {
                        // Use the key to get the translated string
                        var translatedError = localizer[TranslationKeys.At_least_1_Train_Group_Date_is_required];
                        return new ValidationResult(translatedError);
                    }
                    else
                    {
                        // Fallback to raw key if localizer unavailable
                        return new ValidationResult("At least 1 Train Group Date is required!");
                    }
                }
                return ValidationResult.Success;
            }

          
            return new ValidationResult("Invalid list type for this validation.");
        }
    }
}