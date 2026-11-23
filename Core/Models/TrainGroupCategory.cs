using System.Collections.ObjectModel;

namespace Core.Models
{
    // An admin-defined label for grouping train groups, like the user statuses.
    public class TrainGroupCategory : BaseModel
    {
        public string Name { get; set; } = string.Empty;

        // "#rrggbb", shown on both calendars. Empty for categories made before colours
        // existed - the client gives those a preset by id until the admin picks one.
        public string Color { get; set; } = string.Empty;

        public virtual ICollection<TrainGroup> TrainGroups { get; set; } = new Collection<TrainGroup>();
    }
}
