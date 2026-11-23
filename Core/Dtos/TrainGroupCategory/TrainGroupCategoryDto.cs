using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupCategory
{
    // "#rrggbb", or empty to leave it to the client's preset for the category.
    internal static class TrainGroupCategoryColor
    {
        public const string Pattern = "^(#[0-9A-Fa-f]{6})?$";
    }

    public class TrainGroupCategoryDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys._0_cannot_exceed_100_characters)]
        public string Name { get; set; } = string.Empty;

        [RegularExpression(TrainGroupCategoryColor.Pattern, ErrorMessage = TranslationKeys.Invalid_data_provided)]
        public string Color { get; set; } = string.Empty;
    }

    public class TrainGroupCategoryAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys._0_cannot_exceed_100_characters)]
        public string Name { get; set; } = string.Empty;

        [RegularExpression(TrainGroupCategoryColor.Pattern, ErrorMessage = TranslationKeys.Invalid_data_provided)]
        public string Color { get; set; } = string.Empty;
    }
}
