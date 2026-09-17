using Core.Dtos.User;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupΑttendance
{
    public class TrainGroupΑttendanceAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime AttendanceDate { get; set; }

        // An attendance is recorded either against a group, which fills everything below
        // from it, or by hand for a session that no longer has one. One or the other.
        public int? TrainGroupId { get; set; }

        public string TrainGroupTitle { get; set; } = "";
        public string TrainGroupDescription { get; set; } = "";
        public DateTime TrainGroupStartOn { get; set; }
        public DateTime TrainGroupDuration { get; set; }
        public string TrainerFullName { get; set; } = "";

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
