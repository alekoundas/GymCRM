using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupCategory
{
    public class TrainGroupCategoryDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys._0_cannot_exceed_100_characters)]
        public string Name { get; set; } = string.Empty;
    }

    public class TrainGroupCategoryAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys._0_cannot_exceed_100_characters)]
        public string Name { get; set; } = string.Empty;
    }
}
