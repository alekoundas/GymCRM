using Business.Services;
using Core.Dtos;
using Core.Dtos.Questionnaire;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    // The gym's single health questionnaire: the admin writes the questions, every
    // member answers them from their profile.
    [Authorize]
    [Route("api/[controller]")]
    public class QuestionnairesController : ControllerBase
    {
        private const int MaxAnswerLength = 2000;

        private readonly IDataService _dataService;
        private readonly IStringLocalizer _localizer;

        public QuestionnairesController(IDataService dataService, IStringLocalizer localizer)
        {
            _dataService = dataService;
            _localizer = localizer;
        }

        // GET: api/Questionnaires
        // Everybody reads the questions - members answer them. Empty until the admin
        // writes the first one.
        [HttpGet]
        public async Task<ApiResponse<QuestionnaireDto>> Get()
        {
            using ApiDbContext context = _dataService.GetDbContext();

            Questionnaire? questionnaire = await context.Questionnaires
                .AsNoTracking()
                .Include(x => x.Questions)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire));
        }

        // PUT: api/Questionnaires/Name
        [HttpPut("Name")]
        public async Task<ActionResult<ApiResponse<QuestionnaireDto>>> SetName([FromBody] QuestionnaireNameDto dto)
        {
            if (!HasPermission("Questionnaires_Edit"))
                return NotAuthorized<QuestionnaireDto>();

            using ApiDbContext context = _dataService.GetDbContext();
            Questionnaire questionnaire = await GetOrCreateAsync(context);

            questionnaire.Name = (dto.Name ?? string.Empty).Trim();
            await context.SaveChangesAsync();

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire), _localizer[TranslationKeys.Questionnaire_saved]);
        }

        // POST: api/Questionnaires/Questions
        // A new question goes to the end.
        [HttpPost("Questions")]
        public async Task<ActionResult<ApiResponse<QuestionnaireDto>>> AddQuestion([FromBody] QuestionnaireQuestionDto dto)
        {
            if (!HasPermission("Questionnaires_Add"))
                return NotAuthorized<QuestionnaireDto>();

            if (string.IsNullOrWhiteSpace(dto.Title))
                return TitleRequired();

            using ApiDbContext context = _dataService.GetDbContext();
            Questionnaire questionnaire = await GetOrCreateAsync(context);

            QuestionnaireQuestion question = new QuestionnaireQuestion()
            {
                QuestionnaireId = questionnaire.Id,
                OrderNumber = questionnaire.Questions.Count == 0 ? 1 : questionnaire.Questions.Max(x => x.OrderNumber) + 1,
                CreatedBy_Id = User.FindFirst("Id")?.Value ?? string.Empty
            };
            Copy(dto, question);
            questionnaire.Questions.Add(question);
            await context.SaveChangesAsync();

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire), _localizer[TranslationKeys.Questionnaire_saved]);
        }

        // PUT: api/Questionnaires/Questions/5
        [HttpPut("Questions/{id}")]
        public async Task<ActionResult<ApiResponse<QuestionnaireDto>>> UpdateQuestion(int id, [FromBody] QuestionnaireQuestionDto dto)
        {
            if (!HasPermission("Questionnaires_Edit"))
                return NotAuthorized<QuestionnaireDto>();

            if (string.IsNullOrWhiteSpace(dto.Title))
                return TitleRequired();

            using ApiDbContext context = _dataService.GetDbContext();
            Questionnaire questionnaire = await GetOrCreateAsync(context);

            QuestionnaireQuestion? question = questionnaire.Questions.FirstOrDefault(x => x.Id == id);
            if (question == null)
                return NotFoundResponse();

            Copy(dto, question);
            await context.SaveChangesAsync();

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire), _localizer[TranslationKeys.Questionnaire_saved]);
        }

        // DELETE: api/Questionnaires/Questions/5
        // Takes every member's answer to it along, and closes the gap it leaves.
        [HttpDelete("Questions/{id}")]
        public async Task<ActionResult<ApiResponse<QuestionnaireDto>>> DeleteQuestion(int id)
        {
            if (!HasPermission("Questionnaires_Delete"))
                return NotAuthorized<QuestionnaireDto>();

            using ApiDbContext context = _dataService.GetDbContext();
            Questionnaire questionnaire = await GetOrCreateAsync(context);

            QuestionnaireQuestion? question = questionnaire.Questions.FirstOrDefault(x => x.Id == id);
            if (question == null)
                return NotFoundResponse();

            context.QuestionnaireQuestions.Remove(question);
            questionnaire.Questions.Remove(question);
            Renumber(questionnaire);
            await context.SaveChangesAsync();

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire), _localizer[TranslationKeys.Questionnaire_saved]);
        }

        // POST: api/Questionnaires/Questions/5/Move
        // Swaps the question with its neighbour above (-1) or below (+1).
        [HttpPost("Questions/{id}/Move")]
        public async Task<ActionResult<ApiResponse<QuestionnaireDto>>> MoveQuestion(int id, [FromBody] QuestionnaireMoveDto dto)
        {
            if (!HasPermission("Questionnaires_Edit"))
                return NotAuthorized<QuestionnaireDto>();

            using ApiDbContext context = _dataService.GetDbContext();
            Questionnaire questionnaire = await GetOrCreateAsync(context);

            List<QuestionnaireQuestion> ordered = questionnaire.Questions.OrderBy(x => x.OrderNumber).ToList();
            int index = ordered.FindIndex(x => x.Id == id);
            if (index < 0)
                return NotFoundResponse();

            int target = index + Math.Sign(dto.Direction);
            if (target >= 0 && target < ordered.Count)
            {
                (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
                for (int i = 0; i < ordered.Count; i++)
                    ordered[i].OrderNumber = i + 1;
                await context.SaveChangesAsync();
            }

            return new ApiResponse<QuestionnaireDto>().SetSuccessResponse(ToDto(questionnaire));
        }

        // GET: api/Questionnaires/Answers?userId=...
        // A member's own answers. Somebody who may view users can read anybody's.
        [HttpGet("Answers")]
        public async Task<ActionResult<ApiResponse<List<QuestionnaireAnswerDto>>>> GetAnswers([FromQuery] string? userId)
        {
            Guid? targetId = ResolveUser(userId, "Users_View");
            if (targetId == null)
                return NotAuthorized<List<QuestionnaireAnswerDto>>();

            using ApiDbContext context = _dataService.GetDbContext();

            List<QuestionnaireAnswerDto> answers = await context.QuestionnaireAnswers
                .AsNoTracking()
                .Where(x => x.UserId == targetId.Value)
                .Select(x => new QuestionnaireAnswerDto() { QuestionnaireQuestionId = x.QuestionnaireQuestionId, Answer = x.Answer })
                .ToListAsync();

            return new ApiResponse<List<QuestionnaireAnswerDto>>().SetSuccessResponse(answers);
        }

        // PUT: api/Questionnaires/Answers
        // Saves the whole sheet at once. A filled answer is written, a cleared one is
        // removed, and a question left out is left as it was.
        [HttpPut("Answers")]
        public async Task<ActionResult<ApiResponse<List<QuestionnaireAnswerDto>>>> SaveAnswers([FromBody] QuestionnaireAnswersSaveDto dto)
        {
            Guid? targetId = ResolveUser(dto.UserId, "Users_Edit");
            if (targetId == null)
                return NotAuthorized<List<QuestionnaireAnswerDto>>();

            using ApiDbContext context = _dataService.GetDbContext();

            HashSet<int> questionIds = (await context.QuestionnaireQuestions.Select(x => x.Id).ToListAsync()).ToHashSet();
            List<QuestionnaireAnswer> existing = await context.QuestionnaireAnswers
                .Where(x => x.UserId == targetId.Value)
                .ToListAsync();

            IEnumerable<QuestionnaireAnswerDto> incoming = dto.Answers
                .Where(x => questionIds.Contains(x.QuestionnaireQuestionId))
                .GroupBy(x => x.QuestionnaireQuestionId)
                .Select(x => x.Last());

            foreach (QuestionnaireAnswerDto answer in incoming)
            {
                string text = (answer.Answer ?? string.Empty).Trim();
                if (text.Length > MaxAnswerLength)
                    text = text.Substring(0, MaxAnswerLength);

                QuestionnaireAnswer? row = existing.FirstOrDefault(x => x.QuestionnaireQuestionId == answer.QuestionnaireQuestionId);

                if (text.Length == 0)
                {
                    if (row != null)
                        context.QuestionnaireAnswers.Remove(row);
                }
                else if (row == null)
                {
                    context.QuestionnaireAnswers.Add(new QuestionnaireAnswer()
                    {
                        QuestionnaireQuestionId = answer.QuestionnaireQuestionId,
                        UserId = targetId.Value,
                        Answer = text,
                        CreatedBy_Id = User.FindFirst("Id")?.Value ?? string.Empty
                    });
                }
                else if (row.Answer != text)
                {
                    row.Answer = text;
                    row.UpdatedOn = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync();

            List<QuestionnaireAnswerDto> saved = await context.QuestionnaireAnswers
                .AsNoTracking()
                .Where(x => x.UserId == targetId.Value)
                .Select(x => new QuestionnaireAnswerDto() { QuestionnaireQuestionId = x.QuestionnaireQuestionId, Answer = x.Answer })
                .ToListAsync();

            return new ApiResponse<List<QuestionnaireAnswerDto>>().SetSuccessResponse(saved, _localizer[TranslationKeys.Answers_saved]);
        }


        private bool HasPermission(string permission) => User.HasClaim("Permission", permission);

        // The caller, or - with the permission - the member they asked about.
        private Guid? ResolveUser(string? userId, string permission)
        {
            if (!Guid.TryParse(User.FindFirst("Id")?.Value, out Guid callerId))
                return null;

            if (string.IsNullOrWhiteSpace(userId))
                return callerId;

            if (!Guid.TryParse(userId, out Guid requestedId))
                return null;

            return requestedId == callerId || HasPermission(permission) ? requestedId : null;
        }

        private static async Task<Questionnaire> GetOrCreateAsync(ApiDbContext context)
        {
            Questionnaire? questionnaire = await context.Questionnaires
                .Include(x => x.Questions)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (questionnaire != null)
                return questionnaire;

            questionnaire = new Questionnaire();
            context.Questionnaires.Add(questionnaire);
            await context.SaveChangesAsync();
            return questionnaire;
        }

        private static void Copy(QuestionnaireQuestionDto dto, QuestionnaireQuestion question)
        {
            question.Title = dto.Title.Trim();
            question.Details = dto.Details ?? string.Empty;
            question.Image = dto.Image ?? string.Empty;
            question.AnswerPlaceholder = (dto.AnswerPlaceholder ?? string.Empty).Trim();
        }

        private static void Renumber(Questionnaire questionnaire)
        {
            int order = 1;
            foreach (QuestionnaireQuestion question in questionnaire.Questions.OrderBy(x => x.OrderNumber))
                question.OrderNumber = order++;
        }

        private static QuestionnaireDto ToDto(Questionnaire? questionnaire) => new QuestionnaireDto()
        {
            Id = questionnaire?.Id ?? 0,
            Name = questionnaire?.Name ?? string.Empty,
            Questions = (questionnaire?.Questions ?? new List<QuestionnaireQuestion>())
                .OrderBy(x => x.OrderNumber)
                .Select(x => new QuestionnaireQuestionDto()
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    Title = x.Title,
                    Details = x.Details,
                    Image = x.Image,
                    AnswerPlaceholder = x.AnswerPlaceholder
                })
                .ToList()
        };

        private ActionResult NotAuthorized<T>() =>
            BadRequest(new ApiResponse<T>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

        private ActionResult NotFoundResponse() =>
            BadRequest(new ApiResponse<QuestionnaireDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(QuestionnaireQuestion)]));

        private ActionResult TitleRequired() =>
            BadRequest(new ApiResponse<QuestionnaireDto>().SetErrorResponse(_localizer[TranslationKeys._0_is_required, "Title"]));
    }
}
