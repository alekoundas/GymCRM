namespace Core.Dtos.Questionnaire
{
    public class QuestionnaireDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<QuestionnaireQuestionDto> Questions { get; set; } = new List<QuestionnaireQuestionDto>();
    }

    public class QuestionnaireQuestionDto
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string AnswerPlaceholder { get; set; } = string.Empty;
    }

    public class QuestionnaireNameDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class QuestionnaireMoveDto
    {
        // -1 up, +1 down.
        public int Direction { get; set; }
    }

    public class QuestionnaireAnswerDto
    {
        public int QuestionnaireQuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }

    public class QuestionnaireAnswersSaveDto
    {
        // Only somebody who may edit users can save for another member.
        public string? UserId { get; set; }
        public List<QuestionnaireAnswerDto> Answers { get; set; } = new List<QuestionnaireAnswerDto>();
    }
}
