using System.Collections.ObjectModel;

namespace Core.Models
{
    public class QuestionnaireQuestion : BaseModel
    {
        // Position in the questionnaire, from 1.
        public int OrderNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        // The question itself, as html from the rich text editor.
        public string Details { get; set; } = string.Empty;

        // Optional picture - a body chart with numbered areas, say - as a data url.
        public string Image { get; set; } = string.Empty;

        // What the answer box shows before anything is typed, e.g. "18 knee pain".
        public string AnswerPlaceholder { get; set; } = string.Empty;

        public int QuestionnaireId { get; set; }
        public Questionnaire Questionnaire { get; set; } = null!;

        public virtual ICollection<QuestionnaireAnswer> Answers { get; set; } = new Collection<QuestionnaireAnswer>();
    }
}
