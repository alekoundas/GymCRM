using Core.Dtos.User;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupΑttendance
{
    public class TrainGroupΑttendanceAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime AttendanceDate { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
