using Core.Dtos.TrainGroupDate;
using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.CustomValidationAttributes
{
    public class TrainGroupDateTypeValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // If the value is a collection, validate each item
            if (value is IEnumerable<TrainGroupDateAddDto> collectionAdd)
            {
                var errors = new List<string>();
                int index = 0;

                foreach (var dto in collectionAdd)
                {
                    if (dto == null)
                    {
                        errors.Add($"TrainGroupDateAddDto at index {index} is null.");
                        continue;
                    }

                    // Validate based on TrainGroupDateType
                    switch (dto.TrainGroupDateType)
                    {
                        case TrainGroupDateTypeEnum.FIXED_DAY when dto.FixedDay == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: FixedDay is required when TrainGroupDateType is FIXED_DAY.");
                            break;
                        case TrainGroupDateTypeEnum.DAY_OF_MONTH when dto.RecurrenceDayOfMonth == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.");
                            break;
                        case TrainGroupDateTypeEnum.DAY_OF_WEEK when dto.RecurrenceDayOfWeek == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.");
                            break;
                    }

                    // Optional: Add validation for invalid enum values
                    if (!Enum.IsDefined(typeof(TrainGroupDateTypeEnum), dto.TrainGroupDateType))
                    {
                        errors.Add($"TrainGroupDateAddDto at index {index}: Invalid TrainGroupDateType value.");
                    }

                    index++;
                }

                return errors.Count > 0
                    ? new ValidationResult(string.Join("; ", errors))
                    : ValidationResult.Success;
            }
            // If the value is a single TrainGroupDateAddDto
            else if (value is TrainGroupDateAddDto dtoAdd)
            {
                var errors = new List<string>();

                // Validate based on TrainGroupDateType
                switch (dtoAdd.TrainGroupDateType)
                {
                    case TrainGroupDateTypeEnum.FIXED_DAY when dtoAdd.FixedDay == null:
                        errors.Add("FixedDay is required when TrainGroupDateType is FIXED_DAY.");
                        break;
                    case TrainGroupDateTypeEnum.DAY_OF_MONTH when dtoAdd.RecurrenceDayOfMonth == null:
                        errors.Add("RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.");
                        break;
                    case TrainGroupDateTypeEnum.DAY_OF_WEEK when dtoAdd.RecurrenceDayOfWeek == null:
                        errors.Add("RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.");
                        break;
                }

                // Optional: Add validation for invalid enum values
                if (!Enum.IsDefined(typeof(TrainGroupDateTypeEnum), dtoAdd.TrainGroupDateType))
                {
                    errors.Add("Invalid TrainGroupDateType value.");
                }

                return errors.Count > 0
                    ? new ValidationResult(string.Join("; ", errors))
                    : ValidationResult.Success;
            }



            // If the value is a collection, validate each item
            else if (value is IEnumerable<TrainGroupDateDto> collection)
            {
                var errors = new List<string>();
                int index = 0;

                foreach (var dto in collection)
                {
                    if (dto == null)
                    {
                        errors.Add($"TrainGroupDateAddDto at index {index} is null.");
                        continue;
                    }

                    // Validate based on TrainGroupDateType
                    switch (dto.TrainGroupDateType)
                    {
                        case TrainGroupDateTypeEnum.FIXED_DAY when dto.FixedDay == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: FixedDay is required when TrainGroupDateType is FIXED_DAY.");
                            break;
                        case TrainGroupDateTypeEnum.DAY_OF_MONTH when dto.RecurrenceDayOfMonth == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.");
                            break;
                        case TrainGroupDateTypeEnum.DAY_OF_WEEK when dto.RecurrenceDayOfWeek == null:
                            errors.Add($"TrainGroupDateAddDto at index {index}: RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.");
                            break;
                    }

                    // Optional: Add validation for invalid enum values
                    if (!Enum.IsDefined(typeof(TrainGroupDateTypeEnum), dto.TrainGroupDateType))
                    {
                        errors.Add($"TrainGroupDateAddDto at index {index}: Invalid TrainGroupDateType value.");
                    }

                    index++;
                }

                return errors.Count > 0
                    ? new ValidationResult(string.Join("; ", errors))
                    : ValidationResult.Success;
            }
            // If the value is a single TrainGroupDateAddDto
            else if (value is TrainGroupDateDto dto)
            {
                var errors = new List<string>();

                // Validate based on TrainGroupDateType
                switch (dto.TrainGroupDateType)
                {
                    case TrainGroupDateTypeEnum.FIXED_DAY when dto.FixedDay == null:
                        errors.Add("FixedDay is required when TrainGroupDateType is FIXED_DAY.");
                        break;
                    case TrainGroupDateTypeEnum.DAY_OF_MONTH when dto.RecurrenceDayOfMonth == null:
                        errors.Add("RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.");
                        break;
                    case TrainGroupDateTypeEnum.DAY_OF_WEEK when dto.RecurrenceDayOfWeek == null:
                        errors.Add("RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.");
                        break;
                }

                // Optional: Add validation for invalid enum values
                if (!Enum.IsDefined(typeof(TrainGroupDateTypeEnum), dto.TrainGroupDateType))
                {
                    errors.Add("Invalid TrainGroupDateType value.");
                }

                return errors.Count > 0
                    ? new ValidationResult(string.Join("; ", errors))
                    : ValidationResult.Success;
            }
            // If the value is neither, return an error
            else
            {
                return new ValidationResult("Invalid type for validation. Expected TrainGroupDateAddDto or a collection of TrainGroupDateAddDto.");
            }
        }
    }
}