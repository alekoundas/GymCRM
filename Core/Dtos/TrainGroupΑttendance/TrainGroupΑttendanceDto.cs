using Core.Dtos.User;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupΑttendance
{
    public class TrainGroupΑttendanceDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime ΑttendanceDate { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public int TrainGroupId { get; set; }
        //public TrainGroup TrainGroup { get; set; } = null!;

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto User { get; set; } = null!;
    }
}
