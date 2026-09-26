namespace Core.Models
{
    // One member's answer to one question. An answer cleared to nothing is removed.
    public class QuestionnaireAnswer : BaseModel
    {
        public string Answer { get; set; } = string.Empty;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public int QuestionnaireQuestionId { get; set; }
        public QuestionnaireQuestion QuestionnaireQuestion { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
