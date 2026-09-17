using Core.Dtos.TrainGroup;
using Core.Dtos.User;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.TrainGroupΑttendance
{
    public class TrainGroupΑttendanceDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public DateTime AttendanceDate { get; set; }

        // Null once the group is deleted; the snapshot below carries on.
        public int? TrainGroupId { get; set; }
        public TrainGroupDto? TrainGroup { get; set; }

        // What the session was, as it stood on the day. The grid reads these rather than
        // the group, so a row still says what it was after the group is gone or renamed.
        public string TrainGroupTitle { get; set; } = "";
        public string TrainGroupDescription { get; set; } = "";
        public DateTime TrainGroupStartOn { get; set; }
        public DateTime TrainGroupDuration { get; set; }
        public string TrainerFullName { get; set; } = "";
        public Guid? TrainerId { get; set; }


        // When the attendance was taken down, as opposed to the day it is for.
        public DateTime CreatedOn { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
        public UserDto User { get; set; } = null!;
    }
}
