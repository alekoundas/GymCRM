using System.Net;
using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.Mail;
using Core.Enums;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers
{
    public sealed class MailsControllerTests : ApiTest
    {
        private readonly User _maria;
        private readonly User _nikos;
        private readonly Caller _admin;

        public MailsControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User(x => x.Email = "nikos@gym.test");
            _admin = Caller.Of(_nikos, "Mails_View", "Mails_Add", "Mails_Edit", "Mails_Delete");
        }


        // ================================================================
        // Sending
        // ================================================================

        private Task<ApiResult<bool>> SendAsync(Caller caller, params string[] userIds)
        {
            return PostAsync<bool>(caller, "api/Mails/Send", new { subject = "Hello", body = "<p>Body</p>", userIds });
        }

        [Fact]
        public async Task A_mail_is_queued_for_everyone_it_is_for()
        {
            ApiResult<bool> result = await SendAsync(_admin, _maria.Id.ToString(), _nikos.Id.ToString());

            result.ShouldSucceed(English.Of(TranslationKeys._0_emails_queued, "2"));
            result.Data.ShouldBeTrue();
            Emails.Lines.ShouldBe(["queue maria@gym.test, nikos@gym.test: Hello"]);
            Emails.Bodies.ShouldBe(["<p>Body</p>"]);
        }

        [Fact]
        public async Task Somebody_named_twice_or_not_there_is_passed_over()
        {
            (await SendAsync(_admin, _maria.Id.ToString(), _maria.Id.ToString(), Guid.NewGuid().ToString())).ShouldSucceed(English.Of(TranslationKeys._0_emails_queued, "1"));

            Emails.Lines.ShouldBe(["queue maria@gym.test: Hello"]);
        }

        [Fact]
        public async Task A_mail_to_nobody_is_refused()
        {
            (await SendAsync(_admin)).ShouldFail(English.NotFound("User"));
            (await SendAsync(_admin, Guid.NewGuid().ToString())).ShouldFail(English.NotFound("User"));

            Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task Somebody_who_is_no_id_breaks()
        {
            (await SendAsync(_admin, "not an id")).ShouldBreak();
        }

        // It goes out in the gym's name, to anybody.
        [Theory]
        [InlineData("")]
        [InlineData("Mails_View Mails_Edit Mails_Delete")]
        [InlineData("mails_add")]
        [InlineData("Users_Add TrainGroups_Add Roles_Add")]
        public async Task Sending_takes_the_permission_and_no_other_will_do(string permissions)
        {
            Caller caller = Caller.Of(_maria, permissions.Split(' '));

            (await SendAsync(caller, _nikos.Id.ToString())).ShouldFail(English.NotAuthorized);
            (await SendAsync(caller, _maria.Id.ToString())).ShouldFail(English.NotAuthorized);
            (await SendAsync(caller)).ShouldFail(English.NotAuthorized);
            (await SendAsync(caller, "not an id")).ShouldFail(English.NotAuthorized);

            Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task It_takes_no_other_beside_it()
        {
            (await SendAsync(Caller.Of(_maria, "Mails_Add"), _nikos.Id.ToString())).ShouldSucceed(English.Of(TranslationKeys._0_emails_queued, "1"));

            Emails.Lines.ShouldBe(["queue nikos@gym.test: Hello"]);
        }

        [Fact]
        public async Task A_mail_with_something_missing_is_refused()
        {
            (await PostAsync<bool>(_admin, "api/Mails/Send", new { subject = "Hi", body = "", userIds = new[] { _maria.Id } })).ShouldBeBad(
                English.Of(TranslationKeys.Subject_length_must_be_between_5_and_200_character_long), English.Required("Body"));
        }


        // ================================================================
        // Clearing the list
        // ================================================================

        [Fact]
        public async Task Every_mail_is_deleted_at_once()
        {
            Seed.Mail(_maria);
            Seed.Mail(_nikos, x => x.Status = MailStatusEnum.SENT);

            ApiResult<bool> result = await PostAsync<bool>(_admin, "api/Mails/DeleteAll", null);

            result.ShouldSucceed(English.Deleted("Mail"));
            result.Data.ShouldBeTrue();
            Database.Read(db => db.Mails.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task An_empty_list_is_cleared_all_the_same()
        {
            (await PostAsync<bool>(_admin, "api/Mails/DeleteAll", null)).ShouldSucceed(English.Deleted("Mail"));
        }

        [Fact]
        public async Task Clearing_the_list_needs_the_permission_to_delete()
        {
            Seed.Mail(_maria);

            (await PostAsync<bool>(Caller.Of(_nikos, "Mails_View", "Mails_Add", "Mails_Edit"), "api/Mails/DeleteAll", null)).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.Mails.Count()).ShouldBe(1);
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<string> SubjectsAsync(Caller caller, DataTableDto<MailDto> grid)
        {
            DataTableDto<MailDto> answer = (await PostAsync<DataTableDto<MailDto>>(caller, "api/Mails/GetDataTable", grid)).Value;

            answer.TotalRecords.ShouldBe(answer.Data!.Count());

            return string.Join(" ", answer.Data!.Select(x => x.Subject));
        }

        private void AddOneOfEach()
        {
            // Not in the order of their states.
            Seed.Mail(_maria, x => { x.Subject = "sent"; x.Status = MailStatusEnum.SENT; x.SentOn = new DateTime(2031, 3, 5); });
            Seed.Mail(_nikos, x => { x.Subject = "failed"; x.Status = MailStatusEnum.FAILED; x.Error = "No."; });
            Seed.Mail(_maria, x => { x.Subject = "waiting"; x.Status = MailStatusEnum.PENDING; });
        }

        [Fact]
        public async Task Staff_see_every_mail_and_a_member_their_own()
        {
            AddOneOfEach();

            (await SubjectsAsync(_admin, Grid<MailDto>())).ShouldBe("sent failed waiting");
            (await SubjectsAsync(Caller.Of(_maria), Grid<MailDto>())).ShouldBe("sent waiting");
            (await SubjectsAsync(Caller.Of(_nikos, "Mails_Add", "Mails_Edit", "Mails_Delete"), Grid<MailDto>())).ShouldBe("failed");
            (await SubjectsAsync(Caller.WithNoId(), Grid<MailDto>())).ShouldBe("");
        }

        // Whoever the filter names, a member gets no more than their own.
        [Fact]
        public async Task A_filter_does_not_open_somebody_elses()
        {
            AddOneOfEach();

            (await SubjectsAsync(Caller.Of(_maria), Grid<MailDto>(Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals)))).ShouldBe("");
        }

        [Theory]
        [InlineData("PENDING", "waiting")]
        [InlineData("SENT", "sent")]
        [InlineData("FAILED", "failed")]
        [InlineData("sent", "sent")]
        [InlineData("1", "sent")]
        // Not a state, or none named, is no filter.
        [InlineData("other", "sent failed waiting")]
        [InlineData("", "sent failed waiting")]
        [InlineData(null, "sent failed waiting")]
        // A number that is no state has no mails.
        [InlineData("9", "")]
        public async Task Mails_are_filtered_by_their_state(string? status, string expected)
        {
            AddOneOfEach();

            foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.equals, DataTableFiltersEnum.contains, DataTableFiltersEnum.custom })
                foreach (string field in new[] { "status", "Status", "STATUS" })
                    (await SubjectsAsync(_admin, Grid<MailDto>(Filter(field, status, type)))).ShouldBe(expected);
        }

        [Theory]
        [InlineData("status", 1, "waiting sent failed")]
        [InlineData("Status", -1, "failed sent waiting")]
        // Neither up nor down is down.
        [InlineData("status", 0, "failed sent waiting")]
        public async Task Mails_are_sorted_by_their_state(string field, int order, string expected)
        {
            AddOneOfEach();

            (await SubjectsAsync(_admin, SortedGrid<MailDto>((field, order)))).ShouldBe(expected);
        }

        [Fact]
        public async Task Mails_are_sorted_and_filtered_by_anything_else_as_well()
        {
            AddOneOfEach();

            (await SubjectsAsync(_admin, SortedGrid<MailDto>(("subject", 1)))).ShouldBe("failed sent waiting");
            (await SubjectsAsync(_admin, Grid<MailDto>(Filter("subject", "ai"), Filter("status", "FAILED")))).ShouldBe("failed");
        }

        // A sort of its own after the state's starts the order over.
        [Fact]
        public async Task Of_two_sorts_the_other_one_wins_over_the_state()
        {
            AddOneOfEach();

            (await SubjectsAsync(_admin, SortedGrid<MailDto>(("status", 1), ("subject", 1)))).ShouldBe("failed sent waiting");
        }

        [Fact]
        public async Task A_mail_comes_with_its_member_and_without_its_files()
        {
            Seed.Mail(_maria, x => { x.Attachments = "[{\"FileName\":\"a.ics\"}]"; x.Error = "No."; x.SentOn = new DateTime(2031, 3, 5); });

            ApiResult<DataTableDto<MailDto>> result = await PostAsync<DataTableDto<MailDto>>(_admin, "api/Mails/GetDataTable", Grid<MailDto>());

            result.Body.ShouldNotContain("a.ics");
            MailDto mail = result.Value.Data!.Single();
            mail.User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");
            mail.Error.ShouldBe("No.");
            mail.SentOn.ShouldBe(new DateTime(2031, 3, 5));
        }


        // ================================================================
        // What it inherits
        // ================================================================

        [Fact]
        public async Task A_mail_is_read_by_its_id()
        {
            Mail mail = Seed.Mail(_maria, x => x.Subject = "Hello");

            (await GetAsync<MailDto>(_admin, $"api/Mails/{mail.Id}")).Value.Subject.ShouldBe("Hello");
            (await GetAsync<MailDto>(Caller.Of(_maria), $"api/Mails/{mail.Id}")).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task A_mail_can_be_written_down_by_hand()
        {
            ApiResult<List<Mail>> result = await PostAsync<List<Mail>>(_admin, "api/Mails", new[] { new { subject = "Hello", body = "Body", userId = _maria.Id } });

            result.ShouldSucceed(English.Updated("Mail"));
            Mail mail = Database.Read(db => db.Mails.Single());
            mail.UserId.ShouldBe(_maria.Id);
            mail.Status.ShouldBe(MailStatusEnum.PENDING);
        }

        [Fact]
        public async Task A_mail_can_be_edited_and_deleted()
        {
            Mail mail = Seed.Mail(_maria);

            (await PutAsync<Mail>(_admin, $"api/Mails/{mail.Id}", new { id = mail.Id, subject = "Changed", body = "Body", userId = _maria.Id, status = "SENT" })).ShouldSucceed(English.Updated("Mail"));
            Database.Read(db => db.Mails.Single()).Subject.ShouldBe("Changed");
            Database.Read(db => db.Mails.Single()).Status.ShouldBe(MailStatusEnum.SENT);

            (await DeleteAsync<Mail>(_admin, $"api/Mails/{mail.Id}")).ShouldSucceed(English.Deleted("Mail"));
            Database.Read(db => db.Mails.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_state_there_is_not_is_refused()
        {
            Mail mail = Seed.Mail(_maria);

            (await PutAsync<Mail>(_admin, $"api/Mails/{mail.Id}", new { id = mail.Id, subject = "Changed", body = "Body", userId = _maria.Id, status = "OTHER" })).Status.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
