using System.Collections.ObjectModel;

namespace Core.Models
{
    // The gym's one health questionnaire. There is only ever a single row; members
    // answer its questions from their profile.
    public class Questionnaire : BaseModel
    {
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<QuestionnaireQuestion> Questions { get; set; } = new Collection<QuestionnaireQuestion>();
    }
}
