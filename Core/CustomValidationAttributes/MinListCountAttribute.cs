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
                    return new ValidationResult(ErrorMessage);
                }
                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid list.");
        }
    }
}
