using API.Controllers;
using API.Tests.Host;
using Core.Dtos;
using Core.Dtos.Questionnaire;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Mvc;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The gym's one health questionnaire: the questions are the admin's, the answers the members'.
    /// </summary>
    public sealed class QuestionnairesControllerTests : ApiTest
    {
        private const string Url = "api/Questionnaires";
        private const int MaxImageBytes = 3 * 1024 * 1024;

        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;

        public QuestionnairesControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User();
            _nikos = Seed.User();
            _admin = Seed.User();
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, "Questionnaires_View", "Questionnaires_Add", "Questionnaires_Edit", "Questionnaires_Delete"); }
        }

        // Every permission on the questionnaire but the one.
        private Caller Without(string action)
        {
            return Caller.Of(_admin, new[] { "View", "Add", "Edit", "Delete" }.Where(x => x != action).Select(x => "Questionnaires_" + x).ToArray());
        }

        private static string Image(int bytes)
        {
            byte[] image = new byte[bytes];
            Png.CopyTo(image, 0);

            return "data:image/png;base64," + Convert.ToBase64String(image);
        }

        private static string Titles(QuestionnaireDto dto)
        {
            return string.Join(" ", dto.Questions.Select(x => $"{x.OrderNumber}{x.Title}"));
        }

        private string Stored()
        {
            return Database.Read(db => string.Join(" ", db.QuestionnaireQuestions.OrderBy(x => x.OrderNumber).ThenBy(x => x.Id).Select(x => $"{x.OrderNumber}{x.Title}")));
        }

        private QuestionnaireQuestion Question(string title)
        {
            return Database.Read(db => db.QuestionnaireQuestions.Single(x => x.Title == title));
        }


        // ================================================================
        // Reading the questions
        // ================================================================

        [Fact]
        public async Task Everybody_reads_the_questions_in_their_order()
        {
            Database.Add(new Questionnaire
            {
                Name = "Health",
                Questions =
                [
                    new QuestionnaireQuestion { OrderNumber = 2, Title = "Second" },
                    new QuestionnaireQuestion { OrderNumber = 1, Title = "First", Details = "<b>Where</b>", Image = "data:x", AnswerPlaceholder = "18 knee" }
                ]
            });

            ApiResult<QuestionnaireDto> result = await GetAsync<QuestionnaireDto>(Caller.Of(_maria), Url);

            result.ShouldSucceed();
            result.Value.Id.ShouldBe(1);
            result.Value.Name.ShouldBe("Health");
            Titles(result.Value).ShouldBe("1First 2Second");
            QuestionnaireQuestionDto first = result.Value.Questions[0];
            first.Id.ShouldBe(Question("First").Id);
            first.Details.ShouldBe("<b>Where</b>");
            first.Image.ShouldBe("data:x");
            first.AnswerPlaceholder.ShouldBe("18 knee");
        }

        // Nothing is written until the admin does.
        [Fact]
        public async Task Before_there_is_one_it_is_empty()
        {
            QuestionnaireDto dto = (await GetAsync<QuestionnaireDto>(Caller.Of(_maria), Url)).Value;

            dto.Id.ShouldBe(0);
            dto.Name.ShouldBe("");
            dto.Questions.ShouldBeEmpty();
            Database.Read(db => db.Questionnaires.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task The_first_is_the_one()
        {
            Seed.Questionnaire("First");
            Database.Add(new Questionnaire { Name = "Other", Questions = [new QuestionnaireQuestion { OrderNumber = 1, Title = "Other" }] });

            Titles((await GetAsync<QuestionnaireDto>(Caller.Of(_maria), Url)).Value).ShouldBe("1First");
        }


        // ================================================================
        // Its name
        // ================================================================

        private Task<ApiResult<QuestionnaireDto>> NameAsync(Caller caller, string? name)
        {
            return PutAsync<QuestionnaireDto>(caller, Url + "/Name", new { name });
        }

        [Fact]
        public async Task It_is_named_and_comes_to_be_by_it()
        {
            ApiResult<QuestionnaireDto> result = await NameAsync(Admin, "  Health  ");

            result.ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            result.Value.Name.ShouldBe("Health");
            Database.Read(db => db.Questionnaires.Single()).Name.ShouldBe("Health");

            await NameAsync(Admin, "");
            Database.Read(db => db.Questionnaires.Single()).Name.ShouldBe("");
        }

        [Fact]
        public async Task Naming_it_needs_the_edit_permission()
        {
            (await NameAsync(Without("Edit"), "Health")).ShouldBeBad(English.NotAuthorized);

            Database.Read(db => db.Questionnaires.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_name_that_is_left_out_is_refused()
        {
            (await NameAsync(Admin, null)).ShouldBeBad("The Name field is required.");
        }


        // ================================================================
        // Adding a question
        // ================================================================

        private Task<ApiResult<QuestionnaireDto>> AddAsync(Caller caller, string title, string details = "", string image = "", string placeholder = "")
        {
            return PostAsync<QuestionnaireDto>(caller, Url + "/Questions", new { title, details, image, answerPlaceholder = placeholder });
        }

        [Fact]
        public async Task A_question_goes_to_the_end()
        {
            ApiResult<QuestionnaireDto> first = await AddAsync(Admin, "  First  ", "  <b>Where</b>  ", Image(20), "  18 knee  ");
            ApiResult<QuestionnaireDto> second = await AddAsync(Admin, "Second");

            first.ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            Titles(first.Value).ShouldBe("1First");
            Titles(second.Value).ShouldBe("1First 2Second");
            second.Value.Questions[1].Id.ShouldBe(Question("Second").Id);

            QuestionnaireQuestion stored = Question("First");
            // The text as it was written, and the rest without the spaces around it.
            stored.Details.ShouldBe("  <b>Where</b>  ");
            stored.AnswerPlaceholder.ShouldBe("18 knee");
            stored.Image.ShouldBe(Image(20));
            stored.CreatedBy_Id.ShouldBe(_admin.Id.ToString());
            Database.Read(db => db.Questionnaires.Count()).ShouldBe(1);
        }

        // After the last number there is, not after how many there are.
        [Fact]
        public async Task The_end_is_after_the_last_number()
        {
            Seed.Questionnaire("First");
            Database.Execute("UPDATE QuestionnaireQuestions SET OrderNumber = 7");

            Titles((await AddAsync(Admin, "Second")).Value).ShouldBe("7First 8Second");
        }

        // The order and the id are not the form's to say.
        [Fact]
        public async Task Its_place_is_not_taken_from_the_form()
        {
            await PostAsync<QuestionnaireDto>(Admin, Url + "/Questions", new { id = 77, orderNumber = 9, title = "First" });

            Stored().ShouldBe("1First");
            Question("First").Id.ShouldNotBe(77);
        }

        [Fact]
        public async Task Adding_one_needs_the_add_permission()
        {
            (await AddAsync(Without("Add"), "First")).ShouldBeBad(English.NotAuthorized);

            Database.Read(db => db.Questionnaires.Count()).ShouldBe(0);
            (await AddAsync(Caller.WithNoId("Questionnaires_Add"), "First")).ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            Question("First").CreatedBy_Id.ShouldBe("");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_question_needs_a_title(string title)
        {
            Seed.Questionnaire("First");

            (await AddAsync(Admin, title)).ShouldBeBad(English.Required("Title"));
            (await ChangeAsync(Admin, Question("First").Id, title)).ShouldBeBad(English.Required("Title"));

            Stored().ShouldBe("1First");
        }

        [Fact]
        public async Task A_title_that_is_left_out_is_refused()
        {
            (await PostAsync<QuestionnaireDto>(Admin, Url + "/Questions", new { title = (string?)null })).ShouldBeBad("The Title field is required.");
        }

        // Three megabytes, and what it really is.
        [Fact]
        public async Task A_picture_is_one_and_not_too_big()
        {
            Seed.Questionnaire("First");
            int id = Question("First").Id;
            string error = English.Of(TranslationKeys.Image_must_be_a_JPG_PNG_or_WebP_of_up_to_0_MB, "3");

            foreach (string refused in new[] { "x", "data:image/png;base64,AAAA", Image(MaxImageBytes + 1) })
            {
                (await AddAsync(Admin, "Second", image: refused)).ShouldBeBad(error);
                (await ChangeAsync(Admin, id, "Changed", image: refused)).ShouldBeBad(error);
            }

            Stored().ShouldBe("1First");
            (await AddAsync(Admin, "Second", image: Image(MaxImageBytes))).ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            (await ChangeAsync(Admin, id, "Changed", image: Image(MaxImageBytes))).ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
        }


        // ================================================================
        // Changing a question
        // ================================================================

        private Task<ApiResult<QuestionnaireDto>> ChangeAsync(Caller caller, object id, string title, string details = "", string image = "", string placeholder = "")
        {
            return PutAsync<QuestionnaireDto>(caller, $"{Url}/Questions/{id}", new { id = 77, orderNumber = 9, title, details, image, answerPlaceholder = placeholder });
        }

        [Fact]
        public async Task A_question_is_changed_where_it_stands()
        {
            Seed.Questionnaire("First", "Second", "Third");
            QuestionnaireQuestion second = Question("Second");

            ApiResult<QuestionnaireDto> result = await ChangeAsync(Admin, second.Id, " Changed ", "<i>New</i>", Image(20), " 5 ");

            result.ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            Titles(result.Value).ShouldBe("1First 2Changed 3Third");
            Stored().ShouldBe("1First 2Changed 3Third");
            QuestionnaireQuestion changed = Question("Changed");
            changed.Id.ShouldBe(second.Id);
            changed.Details.ShouldBe("<i>New</i>");
            changed.Image.ShouldBe(Image(20));
            changed.AnswerPlaceholder.ShouldBe("5");
        }

        // One that was saved before pictures were looked at does not stop the rest being saved.
        [Fact]
        public async Task A_picture_that_stays_is_not_looked_at_again()
        {
            Seed.Questionnaire("First");
            Database.Execute("UPDATE QuestionnaireQuestions SET Image = 'not a picture'");
            int id = Question("First").Id;

            (await ChangeAsync(Admin, id, "Changed", image: "not a picture")).ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            (await ChangeAsync(Admin, id, "Changed", image: "another")).ShouldBeBad(English.Of(TranslationKeys.Image_must_be_a_JPG_PNG_or_WebP_of_up_to_0_MB, "3"));
            // Taken away.
            (await ChangeAsync(Admin, id, "Changed")).ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));

            Question("Changed").Image.ShouldBe("");
        }

        [Fact]
        public async Task Changing_one_needs_the_edit_permission()
        {
            Seed.Questionnaire("First");

            (await ChangeAsync(Without("Edit"), Question("First").Id, "Changed")).ShouldBeBad(English.NotAuthorized);

            Stored().ShouldBe("1First");
        }

        // Of another questionnaire as well. Looking for one makes the questionnaire.
        [Fact]
        public async Task A_question_that_is_not_there_is_not_found()
        {
            (await ChangeAsync(Admin, 99, "Changed")).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            Database.Read(db => db.Questionnaires.Count()).ShouldBe(1);

            (await DeleteAsync(Admin, 99)).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            (await MoveAsync(Admin, 99, 1)).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            (await ChangeAsync(Admin, "x", "Changed")).ShouldBeBad("The value 'x' is not valid.");

            Questionnaire other = Seed.Questionnaire("Other");
            (await ChangeAsync(Admin, other.Questions.Single().Id, "Changed")).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            (await DeleteAsync(Admin, other.Questions.Single().Id)).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            (await MoveAsync(Admin, other.Questions.Single().Id, 1)).ShouldBeBad(English.NotFound("QuestionnaireQuestion"));
            Stored().ShouldBe("1Other");
        }


        // ================================================================
        // Deleting a question
        // ================================================================

        private Task<ApiResult<QuestionnaireDto>> DeleteAsync(Caller caller, int id)
        {
            return DeleteAsync<QuestionnaireDto>(caller, $"{Url}/Questions/{id}");
        }

        // With every answer to it, and the gap is closed.
        [Theory]
        [InlineData("First", "1Second 2Third")]
        [InlineData("Second", "1First 2Third")]
        [InlineData("Third", "1First 2Second")]
        public async Task A_deleted_question_takes_its_answers_and_leaves_no_gap(string title, string expected)
        {
            Questionnaire questionnaire = Seed.Questionnaire("First", "Second", "Third");
            foreach (QuestionnaireQuestion question in questionnaire.Questions)
                Seed.Answer(question, _maria, question.Title);

            ApiResult<QuestionnaireDto> result = await DeleteAsync(Admin, Question(title).Id);

            result.ShouldSucceed(English.Of(TranslationKeys.Questionnaire_saved));
            Titles(result.Value).ShouldBe(expected);
            Stored().ShouldBe(expected);
            Database.Read(db => db.QuestionnaireAnswers.Select(x => x.Answer).ToList()).ShouldBe(new[] { "First", "Second", "Third" }.Where(x => x != title), ignoreOrder: true);
        }

        [Fact]
        public async Task Deleting_one_needs_the_delete_permission()
        {
            Seed.Questionnaire("First");

            (await DeleteAsync(Without("Delete"), Question("First").Id)).ShouldBeBad(English.NotAuthorized);

            Stored().ShouldBe("1First");
        }


        // ================================================================
        // Moving a question
        // ================================================================

        private Task<ApiResult<QuestionnaireDto>> MoveAsync(Caller caller, int id, int direction)
        {
            return PostAsync<QuestionnaireDto>(caller, $"{Url}/Questions/{id}/Move", new { direction });
        }

        // One place at a time, whichever number is sent, and never past an end.
        [Theory]
        [InlineData("A", 1, "1B 2A 3C")]
        [InlineData("A", -1, "1A 2B 3C")]
        [InlineData("B", 1, "1A 2C 3B")]
        [InlineData("B", -1, "1B 2A 3C")]
        [InlineData("C", 1, "1A 2B 3C")]
        [InlineData("C", -1, "1A 2C 3B")]
        [InlineData("A", 5, "1B 2A 3C")]
        [InlineData("C", -5, "1A 2C 3B")]
        [InlineData("B", int.MinValue, "1B 2A 3C")]
        [InlineData("B", 0, "1A 2B 3C")]
        public async Task A_question_changes_places_with_its_neighbour(string title, int direction, string expected)
        {
            Seed.Questionnaire("A", "B", "C");

            ApiResult<QuestionnaireDto> result = await MoveAsync(Admin, Question(title).Id, direction);

            result.ShouldSucceed();
            Titles(result.Value).ShouldBe(expected);
            Stored().ShouldBe(expected);
        }

        // Gaps are closed by a move that moves something, or that stays where it is.
        [Theory]
        [InlineData(1, "1B 2A")]
        [InlineData(0, "1A 2B")]
        [InlineData(-1, "3A 8B")]
        public async Task The_numbers_are_put_right_along_the_way(int direction, string expected)
        {
            Seed.Questionnaire("A", "B");
            Database.Execute("UPDATE QuestionnaireQuestions SET OrderNumber = CASE Title WHEN 'A' THEN 3 ELSE 8 END");

            await MoveAsync(Admin, Question("A").Id, direction);

            Stored().ShouldBe(expected);
        }

        [Fact]
        public async Task Moving_one_needs_the_edit_permission()
        {
            Seed.Questionnaire("A", "B");

            (await MoveAsync(Without("Edit"), Question("A").Id, 1)).ShouldBeBad(English.NotAuthorized);

            Stored().ShouldBe("1A 2B");
        }


        // ================================================================
        // Reading answers
        // ================================================================

        private Task<ApiResult<List<QuestionnaireAnswerDto>>> AnswersAsync(Caller caller, string query = "")
        {
            return GetAsync<List<QuestionnaireAnswerDto>>(caller, Url + "/Answers" + query);
        }

        [Fact]
        public async Task A_member_reads_their_own_answers()
        {
            Questionnaire questionnaire = Seed.Questionnaire("First", "Second");
            QuestionnaireQuestion first = questionnaire.Questions.First();
            Seed.Answer(first, _maria, "Knee");
            Seed.Answer(first, _nikos, "Back");

            foreach (string query in new[] { "", "?userId=", "?userId=%20", $"?userId={_maria.Id}" })
            {
                ApiResult<List<QuestionnaireAnswerDto>> result = await AnswersAsync(Caller.Of(_maria), query);

                result.ShouldSucceed();
                QuestionnaireAnswerDto answer = result.Value.ShouldHaveSingleItem();
                answer.QuestionnaireQuestionId.ShouldBe(first.Id);
                answer.Answer.ShouldBe("Knee");
            }
        }

        [Fact]
        public async Task Somebody_elses_need_the_permission_to_view_members()
        {
            Seed.Answer(Seed.Questionnaire("First").Questions.Single(), _nikos, "Back");

            (await AnswersAsync(Caller.Of(_maria, "Users_Edit", "Questionnaires_View", "Questionnaires_Edit"), $"?userId={_nikos.Id}")).ShouldBeBad(English.NotAuthorized);
            (await AnswersAsync(Caller.Of(_maria, "Users_View"), $"?userId={_nikos.Id}")).Value.Single().Answer.ShouldBe("Back");
            (await AnswersAsync(Caller.Of(_maria, "Users_View"), $"?userId={Guid.NewGuid()}")).Value.ShouldBeEmpty();
        }

        [Fact]
        public async Task Somebody_who_is_no_id_has_none_to_be_read()
        {
            (await AnswersAsync(Caller.Of(_maria, "Users_View"), "?userId=x")).ShouldBeBad(English.NotAuthorized);
            (await AnswersAsync(Caller.WithNoId("Users_View"), $"?userId={_nikos.Id}")).ShouldBeBad(English.NotAuthorized);
            (await AnswersAsync(Caller.WithNoId("Users_View"))).ShouldBeBad(English.NotAuthorized);
        }


        // ================================================================
        // Saving answers
        // ================================================================

        private Task<ApiResult<List<QuestionnaireAnswerDto>>> SaveAsync(Caller caller, string? userId, params (int QuestionId, string Answer)[] answers)
        {
            return PutAsync<List<QuestionnaireAnswerDto>>(caller, Url + "/Answers", new
            {
                userId,
                answers = answers.Select(x => new { questionnaireQuestionId = x.QuestionId, answer = x.Answer })
            });
        }

        private List<QuestionnaireAnswer> AnswersOf(User user)
        {
            return Database.Read(db => db.QuestionnaireAnswers.Where(x => x.UserId == user.Id).OrderBy(x => x.QuestionnaireQuestionId).ToList());
        }

        // One written, one changed, one cleared, one the same and one left out.
        [Fact]
        public async Task The_whole_sheet_is_saved_at_once()
        {
            DateTime old = new DateTime(2025, 1, 2);
            QuestionnaireQuestion[] questions = Seed.Questionnaire("New", "Changed", "Cleared", "Same", "Left out").Questions.ToArray();
            foreach (QuestionnaireQuestion question in questions.Skip(1))
                Seed.Answer(question, _maria, "before");
            Seed.Answer(questions[1], _nikos, "before");
            Database.Change(db => { foreach (QuestionnaireAnswer answer in db.QuestionnaireAnswers) answer.UpdatedOn = old; });
            DateTime before = DateTime.UtcNow;

            ApiResult<List<QuestionnaireAnswerDto>> result = await SaveAsync(Caller.Of(_maria), null,
                (questions[0].Id, "  first  "), (questions[1].Id, "after"), (questions[2].Id, "  "), (questions[3].Id, " before "));

            result.ShouldSucceed(English.Of(TranslationKeys.Answers_saved));
            result.Value.OrderBy(x => x.QuestionnaireQuestionId).Select(x => x.Answer).ShouldBe(["first", "after", "before", "before"]);

            List<QuestionnaireAnswer> saved = AnswersOf(_maria);
            saved.Select(x => x.QuestionnaireQuestionId).ShouldBe([questions[0].Id, questions[1].Id, questions[3].Id, questions[4].Id]);
            saved[0].Answer.ShouldBe("first");
            saved[0].CreatedBy_Id.ShouldBe(_maria.Id.ToString());
            saved[0].UpdatedOn.ShouldBeInRange(before, DateTime.UtcNow);
            saved[1].Answer.ShouldBe("after");
            saved[1].UpdatedOn.ShouldBeInRange(before, DateTime.UtcNow);
            saved[2].UpdatedOn.ShouldBe(old);
            saved[3].UpdatedOn.ShouldBe(old);
            AnswersOf(_nikos).Single().Answer.ShouldBe("before");
        }

        [Fact]
        public async Task An_answer_that_was_never_given_is_not_cleared()
        {
            QuestionnaireQuestion question = Seed.Questionnaire("First").Questions.Single();

            (await SaveAsync(Caller.Of(_maria), null, (question.Id, ""))).Value.ShouldBeEmpty();
            (await SaveAsync(Caller.Of(_maria), null)).Value.ShouldBeEmpty();
        }

        // Of the same question the last counts, and one that is not there for nothing.
        [Fact]
        public async Task Each_question_is_answered_once()
        {
            QuestionnaireQuestion question = Seed.Questionnaire("First").Questions.Single();

            (await SaveAsync(Caller.Of(_maria), null, (question.Id, "one"), (99, "none"), (question.Id, "two"))).ShouldSucceed(English.Of(TranslationKeys.Answers_saved));

            AnswersOf(_maria).Single().Answer.ShouldBe("two");
        }

        [Theory]
        [InlineData(1999, 1999)]
        [InlineData(2000, 2000)]
        [InlineData(2001, 2000)]
        [InlineData(5000, 2000)]
        public async Task An_answer_is_cut_at_two_thousand_characters(int length, int kept)
        {
            QuestionnaireQuestion question = Seed.Questionnaire("First").Questions.Single();

            string answer = "A" + new string('x', length - 2) + "Z";

            await SaveAsync(Caller.Of(_maria), null, (question.Id, $"  {answer}  "));

            AnswersOf(_maria).Single().Answer.ShouldBe(answer.Substring(0, kept));
        }

        [Fact]
        public async Task Saving_for_somebody_else_needs_the_permission_to_edit_members()
        {
            QuestionnaireQuestion question = Seed.Questionnaire("First").Questions.Single();
            string nikos = _nikos.Id.ToString();

            (await SaveAsync(Caller.Of(_maria, "Users_View", "Questionnaires_Edit"), nikos, (question.Id, "x"))).ShouldBeBad(English.NotAuthorized);
            (await SaveAsync(Caller.Of(_maria, "Users_Edit"), "x", (question.Id, "x"))).ShouldBeBad(English.NotAuthorized);
            (await SaveAsync(Caller.WithNoId("Users_Edit"), nikos, (question.Id, "x"))).ShouldBeBad(English.NotAuthorized);
            AnswersOf(_nikos).ShouldBeEmpty();

            (await SaveAsync(Caller.Of(_maria, "Users_Edit"), nikos, (question.Id, "Back"))).ShouldSucceed(English.Of(TranslationKeys.Answers_saved));
            AnswersOf(_nikos).Single().Answer.ShouldBe("Back");
            AnswersOf(_nikos).Single().CreatedBy_Id.ShouldBe(_maria.Id.ToString());

            // Themselves they may name.
            (await SaveAsync(Caller.Of(_nikos), nikos, (question.Id, "Neck"))).ShouldSucceed(English.Of(TranslationKeys.Answers_saved));
            AnswersOf(_nikos).Single().Answer.ShouldBe("Neck");
        }

        [Fact]
        public async Task Saving_for_somebody_who_is_not_there_breaks()
        {
            QuestionnaireQuestion question = Seed.Questionnaire("First").Questions.Single();

            (await SaveAsync(Caller.Of(_maria, "Users_Edit"), Guid.NewGuid().ToString(), (question.Id, "x"))).ShouldBreak();
        }

        [Fact]
        public async Task An_answer_that_is_left_out_is_refused()
        {
            (await PutAsync<List<QuestionnaireAnswerDto>>(Caller.Of(_maria), Url + "/Answers", new { answers = new[] { new { questionnaireQuestionId = 1, answer = (string?)null } } }))
                .ShouldBeBad("The Answer field is required.");
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // Json that says null is refused before it gets here.
        [Fact]
        public async Task What_is_missing_is_taken_for_empty()
        {
            QuestionnairesController controller = Direct.Controller<QuestionnairesController>(Services, "Questionnaires", ["Questionnaires_Add", "Questionnaires_Edit"], _maria);

            (await controller.SetName(new QuestionnaireNameDto { Name = null! })).Value.ShouldNotBeNull().Data.ShouldNotBeNull().Name.ShouldBe("");

            ActionResult<ApiResponse<QuestionnaireDto>> added = await controller.AddQuestion(new QuestionnaireQuestionDto { Title = "First", Details = null!, Image = null!, AnswerPlaceholder = null! });
            QuestionnaireQuestionDto question = added.Value.ShouldNotBeNull().Data.ShouldNotBeNull().Questions.Single();
            (question.Details, question.Image, question.AnswerPlaceholder).ShouldBe(("", "", ""));

            await controller.SaveAnswers(new QuestionnaireAnswersSaveDto { Answers = [new QuestionnaireAnswerDto { QuestionnaireQuestionId = question.Id, Answer = "x" }] });
            await controller.SaveAnswers(new QuestionnaireAnswersSaveDto { Answers = [new QuestionnaireAnswerDto { QuestionnaireQuestionId = question.Id, Answer = null! }] });
            AnswersOf(_maria).ShouldBeEmpty();
        }
    }
}
