using API.Controllers;
using API.Tests.Host;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Subscription;
using Core.Enums;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Lessons asked for, granted, refused and taken back.
    /// </summary>
    public sealed class SubscriptionsControllerTests : ApiTest
    {
        private const string Url = "api/Subscriptions";

        private static readonly string[] Everything =
        [
            "Subscriptions_View", "Subscriptions_Add", "Subscriptions_Edit", "Subscriptions_Delete",
            "SubscriptionsAdmin_View", "SubscriptionsAdmin_Add", "SubscriptionsAdmin_Edit", "SubscriptionsAdmin_Delete"
        ];

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;

        public SubscriptionsControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User(x => x.Email = "nikos@gym.test");
            _admin = Seed.User();
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, Everything); }
        }

        private Caller Member
        {
            get { return Caller.Of(_maria, "Subscriptions_View", "Subscriptions_Add", "Subscriptions_Delete"); }
        }

        // Every permission but one.
        private Caller Without(string permission)
        {
            return Caller.Of(_admin, Everything.Where(x => x != permission).ToArray());
        }

        private List<Subscription> All()
        {
            return Database.Read(db => db.Subscriptions.OrderBy(x => x.Id).ToList());
        }

        private Subscription Read(int id)
        {
            return All().Single(x => x.Id == id);
        }


        // ================================================================
        // Closed
        // ================================================================

        // They take a whole row, and would let anybody write themselves an approved one.
        [Fact]
        public async Task Adding_and_changing_rows_outright_is_refused_to_everybody()
        {
            Subscription there = Seed.Subscription(_maria, 5);

            (await PostAsync<List<Subscription>>(Admin, Url, new[] { new { userId = _maria.Id, amount = 100 } })).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<Subscription>>(Admin, Url, Array.Empty<object>())).ShouldFail(English.NotAuthorized);
            (await PutAsync<Subscription>(Admin, $"{Url}/{there.Id}", new { id = there.Id, userId = _maria.Id, amount = 100 })).ShouldFail(English.NotAuthorized);

            All().Single().Amount.ShouldBe(5);
        }

        // Still held to what a row must look like.
        [Fact]
        public async Task And_a_bad_row_is_a_bad_request_first()
        {
            (await PostAsync<List<Subscription>>(Admin, Url, new[] { new { userId = "", amount = 100 } })).ShouldBeBad(English.Required("UserId"));
        }


        // ================================================================
        // Granting
        // ================================================================

        private Task<ApiResult<SubscriptionDto>> GrantAsync(Caller caller, object userId, int amount, string comment = "", bool notifyUser = false, string? language = null)
        {
            return PostAsync<SubscriptionDto>(caller, Url + "/Add", new { userId, amount, adminComment = comment, notifyUser }, language);
        }

        // Negative ones as well: a correction.
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(-3)]
        [InlineData(int.MaxValue)]
        public async Task Lessons_are_granted_as_approved_at_once(int amount)
        {
            DateTime before = DateTime.UtcNow;

            ApiResult<SubscriptionDto> result = await GrantAsync(Admin, _maria.Id, amount, "Welcome");

            result.ShouldSucceed();
            Subscription subscription = All().Single();
            subscription.UserId.ShouldBe(_maria.Id);
            subscription.Amount.ShouldBe(amount);
            subscription.RequestedAmount.ShouldBeNull();
            subscription.Status.ShouldBe(SubscriptionStatusEnum.APPROVED);
            subscription.AdminComment.ShouldBe("Welcome");
            subscription.MemberComment.ShouldBe("");
            subscription.DecidedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            subscription.CreatedOn.ShouldBe(subscription.DecidedOn.Value);
            subscription.CreatedBy_Id.ShouldBe(_admin.Id.ToString());

            result.Value.Id.ShouldBe(subscription.Id);
            result.Value.Amount.ShouldBe(amount);
            result.Value.Status.ShouldBe(SubscriptionStatusEnum.APPROVED);
            result.Value.UserId.ShouldBe(_maria.Id.ToString());
            result.Value.User.ShouldBeNull();
            Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task None_at_all_is_not_a_grant()
        {
            (await GrantAsync(Admin, _maria.Id, 0)).ShouldFail(English.Of(TranslationKeys._0_must_be_a_positive_number, "Amount"));

            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task Granting_needs_the_admins_add_permission()
        {
            (await GrantAsync(Without("SubscriptionsAdmin_Add"), _maria.Id, 5)).ShouldFail(English.NotAuthorized);
            // Asked before anything else is.
            (await GrantAsync(Without("SubscriptionsAdmin_Add"), "x", 0)).ShouldFail(English.NotAuthorized);

            All().ShouldBeEmpty();
            (await GrantAsync(Caller.Of(_admin, "SubscriptionsAdmin_Add"), _maria.Id, 5)).ShouldSucceed();
        }

        [Fact]
        public async Task The_member_has_to_be_somebody()
        {
            (await GrantAsync(Admin, "", 5)).ShouldBeBad(English.Required("UserId"));
            (await GrantAsync(Admin, "x", 5)).ShouldFail(English.NotFound("User"));
            (await GrantAsync(Admin, Guid.NewGuid(), 5)).ShouldBreak();

            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task A_grant_by_a_token_with_no_id_is_nobodys()
        {
            await GrantAsync(Caller.WithNoId("SubscriptionsAdmin_Add"), _maria.Id, 5);

            All().Single().CreatedBy_Id.ShouldBe("");
        }

        [Fact]
        public async Task A_grant_says_what_was_added_and_what_is_left()
        {
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            await GrantAsync(Admin, _maria.Id, 10, "See <b>you</b> & yours", notifyUser: true);

            Emails.Lines.ShouldBe([$"queue maria@gym.test: {English.Of(TranslationKeys.Subscription_Renewed)}"]);
            ApprovedFile.ShouldMatch(Emails.Bodies.Single(), "SubscriptionGranted");
        }

        [Fact]
        public async Task The_mail_is_in_the_language_of_the_admin()
        {
            await GrantAsync(Admin, _maria.Id, 10, notifyUser: true, language: "el");

            Emails.Lines.ShouldBe([$"queue maria@gym.test: {Language.Text("el", TranslationKeys.Subscription_Renewed)}"]);
            Emails.Bodies.Single().ShouldContain(Language.Text("el", TranslationKeys.Subscriptions_were_added_to_your_account));
        }

        // A minus is never shown: what is owed reads as lessons missing.
        [Theory]
        [InlineData(1, 3, TranslationKeys.Missing_subscriptions, "2")]
        [InlineData(2, 2, TranslationKeys.Remaining_subscriptions, "0")]
        [InlineData(3, 2, TranslationKeys.Remaining_subscriptions, "1")]
        public async Task What_is_owed_reads_as_missing(int amount, int attended, string label, string number)
        {
            for (int i = 0; i < attended; i++)
                Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            await GrantAsync(Admin, _maria.Id, amount, notifyUser: true);

            Emails.Bodies.Single().ShouldContain($"<li><strong>{English.Of(label)}:</strong> {number}</li>");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Without_a_comment_there_is_no_note(string comment)
        {
            await GrantAsync(Admin, _maria.Id, 5, comment, notifyUser: true);

            Emails.Bodies.Single().ShouldNotContain(English.Of(TranslationKeys.Note_from_your_trainer));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Somebody_with_no_address_is_not_written_to(string email)
        {
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).Email = email);

            (await GrantAsync(Admin, _maria.Id, 5, notifyUser: true)).ShouldSucceed();

            Emails.Lines.ShouldBeEmpty();
            All().Count.ShouldBe(1);
        }

        // The account can only go in the moment between the row being written and the mail.
        [Fact]
        public async Task Somebody_whose_account_is_gone_is_not_written_to()
        {
            IDataService data = ActivatorUtilities.CreateInstance<DataService>(Services.CreateScope().ServiceProvider, new ForgetfulSubscriptions(Database.Factory));
            SubscriptionsController controller = Direct.Controller<SubscriptionsController>(Services, "Subscriptions", ["SubscriptionsAdmin_Add"], null, data);

            ApiResponse<SubscriptionDto> response = await controller.Add(new SubscriptionAddDto { UserId = Guid.NewGuid().ToString(), Amount = 5, NotifyUser = true });

            response.IsSucceed.ShouldBeTrue();
            Emails.Lines.ShouldBeEmpty();
        }

        // Takes the row and writes nothing, so that the member may be nobody.
        private sealed class ForgetfulSubscriptions : GenericRepository<Subscription>, IGenericRepository<Subscription>
        {
            public ForgetfulSubscriptions(IDbContextFactory<ApiDbContext> factory) : base(factory)
            {
            }

            Task<int> IGenericRepository<Subscription>.AddAsync(Subscription entity)
            {
                return Task.FromResult(1);
            }
        }


        // ================================================================
        // Asking
        // ================================================================

        private Task<ApiResult<SubscriptionDto>> RequestAsync(Caller caller, int amount, string comment = "")
        {
            return PostAsync<SubscriptionDto>(caller, Url + "/Request", new { requestedAmount = amount, memberComment = comment });
        }

        [Fact]
        public async Task A_member_asks_and_is_given_nothing_yet()
        {
            DateTime before = DateTime.UtcNow;

            ApiResult<SubscriptionDto> result = await RequestAsync(Member, 10, "Please");

            result.ShouldSucceed();
            Subscription subscription = All().Single();
            subscription.UserId.ShouldBe(_maria.Id);
            subscription.RequestedAmount.ShouldBe(10);
            subscription.Amount.ShouldBe(0);
            subscription.Status.ShouldBe(SubscriptionStatusEnum.PENDING);
            subscription.MemberComment.ShouldBe("Please");
            subscription.AdminComment.ShouldBe("");
            subscription.DecidedOn.ShouldBeNull();
            subscription.CreatedOn.ShouldBeInRange(before, DateTime.UtcNow);
            subscription.CreatedBy_Id.ShouldBe(_maria.Id.ToString());
            result.Value.Id.ShouldBe(subscription.Id);
            result.Value.RequestedAmount.ShouldBe(10);
            Emails.Lines.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public async Task A_member_asks_for_as_many_as_they_like(int amount)
        {
            (await RequestAsync(Member, amount)).Value.RequestedAmount.ShouldBe(amount);
        }

        // Whatever else is in the request.
        [Fact]
        public async Task Nothing_sent_along_can_grant_anything()
        {
            await PostAsync<SubscriptionDto>(Member, Url + "/Request", new { requestedAmount = 10, amount = 100, status = "APPROVED", userId = _nikos.Id, decidedOn = DateTime.UtcNow, adminComment = "x" });

            Subscription subscription = All().Single();
            subscription.Amount.ShouldBe(0);
            subscription.Status.ShouldBe(SubscriptionStatusEnum.PENDING);
            subscription.UserId.ShouldBe(_maria.Id);
            subscription.DecidedOn.ShouldBeNull();
            subscription.AdminComment.ShouldBe("");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public async Task It_has_to_ask_for_something(int amount)
        {
            (await RequestAsync(Member, amount)).ShouldFail(English.Of(TranslationKeys._0_must_be_a_positive_number, "RequestedAmount"));

            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task Asking_needs_the_add_permission_and_an_id()
        {
            (await RequestAsync(Without("Subscriptions_Add"), 5)).ShouldFail(English.NotAuthorized);
            (await RequestAsync(Caller.WithNoId("Subscriptions_Add"), 5)).ShouldFail(English.NotSignedIn);

            All().ShouldBeEmpty();
        }

        // One at a time. What was decided, or is somebody else's, is not in the way.
        [Theory]
        [InlineData(SubscriptionStatusEnum.PENDING, false)]
        [InlineData(SubscriptionStatusEnum.APPROVED, true)]
        [InlineData(SubscriptionStatusEnum.REJECTED, true)]
        public async Task A_member_asks_once_until_they_are_answered(SubscriptionStatusEnum status, bool isAllowed)
        {
            Seed.Subscription(_maria, 5, status);
            Seed.Subscription(_nikos, 5, SubscriptionStatusEnum.PENDING);

            ApiResult<SubscriptionDto> result = await RequestAsync(Member, 5);

            if (isAllowed)
                result.ShouldSucceed();
            else
                result.ShouldFail(English.Of(TranslationKeys.A_request_is_already_pending));
        }


        // ================================================================
        // Deciding
        // ================================================================

        private Subscription Pending(int requested = 10)
        {
            return Seed.Subscription(_maria, 0, SubscriptionStatusEnum.PENDING, x => { x.RequestedAmount = requested; x.MemberComment = "Please"; x.CreatedBy_Id = "maker"; x.CreatedOn = new DateTime(2025, 1, 2); });
        }

        private Task<ApiResult<SubscriptionDto>> DecideAsync(Caller caller, int id, bool isApproved, int amount = 0, string comment = "", bool notifyUser = false)
        {
            return PostAsync<SubscriptionDto>(caller, Url + "/Decide", new { id, isApproved, amount, adminComment = comment, notifyUser });
        }

        // What is granted need not be what was asked for, and a refusal grants nothing whatever it says.
        [Theory]
        [InlineData(true, 10, SubscriptionStatusEnum.APPROVED, 10)]
        [InlineData(true, 3, SubscriptionStatusEnum.APPROVED, 3)]
        [InlineData(true, 99, SubscriptionStatusEnum.APPROVED, 99)]
        [InlineData(false, 0, SubscriptionStatusEnum.REJECTED, 0)]
        [InlineData(false, 10, SubscriptionStatusEnum.REJECTED, 0)]
        [InlineData(false, -10, SubscriptionStatusEnum.REJECTED, 0)]
        public async Task A_request_is_granted_or_refused(bool isApproved, int amount, SubscriptionStatusEnum status, int granted)
        {
            Subscription pending = Pending();
            DateTime before = DateTime.UtcNow;

            ApiResult<SubscriptionDto> result = await DecideAsync(Admin, pending.Id, isApproved, amount, "Done");

            result.ShouldSucceed();
            result.Value.Status.ShouldBe(status);
            result.Value.Amount.ShouldBe(granted);
            result.Value.User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");

            Subscription decided = All().Single();
            decided.Status.ShouldBe(status);
            decided.Amount.ShouldBe(granted);
            decided.AdminComment.ShouldBe("Done");
            decided.DecidedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            decided.RequestedAmount.ShouldBe(10);
            decided.MemberComment.ShouldBe("Please");
            decided.UserId.ShouldBe(_maria.Id);
            decided.CreatedBy_Id.ShouldBe("maker");
            decided.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            Emails.Lines.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Nothing_cannot_be_granted(int amount)
        {
            Subscription pending = Pending();

            (await DecideAsync(Admin, pending.Id, true, amount)).ShouldFail(English.Of(TranslationKeys._0_must_be_a_positive_number, "Amount"));

            Read(pending.Id).Status.ShouldBe(SubscriptionStatusEnum.PENDING);
        }

        // A second press cannot grant the lessons twice.
        [Theory]
        [InlineData(SubscriptionStatusEnum.APPROVED)]
        [InlineData(SubscriptionStatusEnum.REJECTED)]
        public async Task What_was_decided_stays_decided(SubscriptionStatusEnum status)
        {
            Subscription decided = Seed.Subscription(_maria, 5, status);

            (await DecideAsync(Admin, decided.Id, true, 10)).ShouldFail(English.Of(TranslationKeys.The_request_has_already_been_decided));
            (await DecideAsync(Admin, decided.Id, false)).ShouldFail(English.Of(TranslationKeys.The_request_has_already_been_decided));
            // Said before that the amount is wrong.
            (await DecideAsync(Admin, decided.Id, true, 0)).ShouldFail(English.Of(TranslationKeys.The_request_has_already_been_decided));

            Read(decided.Id).Amount.ShouldBe(5);
            Read(decided.Id).Status.ShouldBe(status);
        }

        [Fact]
        public async Task Deciding_needs_the_admins_edit_permission()
        {
            Subscription pending = Pending();

            (await DecideAsync(Without("SubscriptionsAdmin_Edit"), pending.Id, true, 10)).ShouldFail(English.NotAuthorized);
            (await DecideAsync(Member, pending.Id, true, 10)).ShouldFail(English.NotAuthorized);

            Read(pending.Id).Status.ShouldBe(SubscriptionStatusEnum.PENDING);
            (await DecideAsync(Caller.Of(_admin, "SubscriptionsAdmin_Edit"), pending.Id, true, 10)).ShouldSucceed();
        }

        [Fact]
        public async Task A_request_that_is_not_there_is_not_found()
        {
            (await DecideAsync(Admin, 99, true, 10)).ShouldFail(English.NotFound("Subscription"));
        }

        [Fact]
        public async Task The_members_account_is_not_written_along_with_it()
        {
            Subscription pending = Pending();
            User before = Database.Read(db => db.Users.Single(x => x.Id == _maria.Id));

            await DecideAsync(Admin, pending.Id, true, 10);

            User after = Database.Read(db => db.Users.Single(x => x.Id == _maria.Id));
            after.ConcurrencyStamp.ShouldBe(before.ConcurrencyStamp);
            after.SecurityStamp.ShouldBe(before.SecurityStamp);
        }

        [Fact]
        public async Task A_yes_says_what_was_asked_what_was_given_and_what_is_left()
        {
            await DecideAsync(Admin, Pending().Id, true, 8, "Enjoy <3", notifyUser: true);

            Emails.Lines.ShouldBe([$"queue maria@gym.test: {English.Of(TranslationKeys.Subscription_Renewed)}"]);
            ApprovedFile.ShouldMatch(Emails.Bodies.Single(), "SubscriptionApproved");
        }

        [Fact]
        public async Task A_no_says_what_to_do_next()
        {
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            await DecideAsync(Admin, Pending().Id, false, 0, "Not now", notifyUser: true);

            Emails.Lines.ShouldBe([$"queue maria@gym.test: {English.Of(TranslationKeys.Subscription_Request_Answered)}"]);
            ApprovedFile.ShouldMatch(Emails.Bodies.Single(), "SubscriptionRejected");
        }

        // An old row that does not say how many were asked for.
        [Fact]
        public async Task Without_an_amount_asked_for_there_is_no_line_for_it()
        {
            Subscription pending = Seed.Subscription(_maria, 0, SubscriptionStatusEnum.PENDING);

            await DecideAsync(Admin, pending.Id, true, 8, notifyUser: true);

            Emails.Bodies.Single().ShouldNotContain(English.Of(TranslationKeys.Subscriptions_requested));
            Emails.Bodies.Single().ShouldContain($"<strong>{English.Of(TranslationKeys.Subscriptions_added)}:</strong> 8</li>");
        }

        [Fact]
        public async Task Somebody_with_no_address_is_not_told()
        {
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).Email = "");

            (await DecideAsync(Admin, Pending().Id, true, 8, notifyUser: true)).ShouldSucceed();

            Emails.Lines.ShouldBeEmpty();
        }


        // ================================================================
        // Taking back
        // ================================================================

        private Task<ApiResult<bool>> RemoveAsync(Caller caller, int id, bool notifyUser = false)
        {
            return PostAsync<bool>(caller, Url + "/Remove", new { id, notifyUser });
        }

        [Theory]
        [InlineData(SubscriptionStatusEnum.PENDING)]
        [InlineData(SubscriptionStatusEnum.APPROVED)]
        [InlineData(SubscriptionStatusEnum.REJECTED)]
        public async Task An_entry_is_taken_back_whatever_became_of_it(SubscriptionStatusEnum status)
        {
            Subscription subscription = Seed.Subscription(_maria, 5, status);
            Subscription other = Seed.Subscription(_maria, 7);

            ApiResult<bool> result = await RemoveAsync(Admin, subscription.Id);

            result.ShouldSucceed(English.Deleted("Subscription"));
            result.Data.ShouldBeTrue();
            All().Single().Id.ShouldBe(other.Id);
            Database.Read(db => db.Users.Any(x => x.Id == _maria.Id)).ShouldBeTrue();
            Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task Taking_back_needs_the_admins_delete_permission()
        {
            Subscription subscription = Seed.Subscription(_maria, 5);

            (await RemoveAsync(Without("SubscriptionsAdmin_Delete"), subscription.Id)).ShouldFail(English.NotAuthorized);
            All().Count.ShouldBe(1);

            (await RemoveAsync(Caller.Of(_admin, "SubscriptionsAdmin_Delete"), subscription.Id)).ShouldSucceed(English.Deleted("Subscription"));
        }

        [Fact]
        public async Task An_entry_that_is_not_there_is_not_found()
        {
            (await RemoveAsync(Admin, 99)).ShouldFail(English.NotFound("Subscription"));
        }

        [Fact]
        public async Task The_member_is_told_what_was_taken_and_what_is_left()
        {
            Seed.Subscription(_maria, 3);
            Subscription subscription = Seed.Subscription(_maria, 5, change: x => x.AdminComment = "Wrong one & sorry");
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            await RemoveAsync(Admin, subscription.Id, notifyUser: true);

            Emails.Lines.ShouldBe([$"queue maria@gym.test: {English.Of(TranslationKeys.Subscription_Adjusted)}"]);
            ApprovedFile.ShouldMatch(Emails.Bodies.Single(), "SubscriptionRemoved");
        }

        // Nothing was counted, so there is no number worth giving. Nor for a correction, which is less
        // than nothing.
        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-4, false)]
        public async Task Only_a_number_above_nought_is_given(int amount, bool isGiven)
        {
            Subscription subscription = Seed.Subscription(_maria, amount);

            await RemoveAsync(Admin, subscription.Id, notifyUser: true);

            Emails.Bodies.Single().Contains(English.Of(TranslationKeys.Subscriptions_removed) + ":").ShouldBe(isGiven);
            Emails.Bodies.Single().ShouldContain($"<strong>{English.Of(TranslationKeys.Remaining_subscriptions)}:</strong> 0</li>");
        }


        // ================================================================
        // How many are left
        // ================================================================

        private Task<ApiResult<int>> BalanceAsync(Caller caller, string query = "")
        {
            return GetAsync<int>(caller, Url + "/Balance" + query);
        }

        [Fact]
        public async Task A_member_asks_after_themselves()
        {
            Seed.Subscription(_maria, 10);
            Seed.Subscription(_maria, 50, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_nikos, 99);
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            foreach (string query in new[] { "", "?userId=", "?userId=%20%20", $"?userId={_maria.Id}", $"?userId={_maria.Id.ToString().ToUpperInvariant()}" })
            {
                // No permission is needed for that.
                ApiResult<int> result = await BalanceAsync(Caller.Of(_maria), query);

                result.ShouldSucceed();
                result.Data.ShouldBe(9, query);
            }
        }

        [Fact]
        public async Task Asking_after_somebody_else_needs_the_admins_view_permission()
        {
            Seed.Subscription(_nikos, 99);

            (await BalanceAsync(Without("SubscriptionsAdmin_View"), $"?userId={_nikos.Id}")).ShouldFail(English.NotAuthorized);
            (await BalanceAsync(Caller.Of(_maria, "SubscriptionsAdmin_View"), $"?userId={_nikos.Id}")).Data.ShouldBe(99);
        }

        [Fact]
        public async Task Somebody_who_is_not_there_has_none()
        {
            (await BalanceAsync(Admin, $"?userId={Guid.NewGuid()}")).Data.ShouldBe(0);
            (await BalanceAsync(Caller.Of(_maria))).Data.ShouldBe(0);
            (await BalanceAsync(Admin, "?userId=x")).ShouldFail(English.NotFound("User"));
            (await BalanceAsync(Caller.Of(_maria), "?userId=x")).ShouldFail(English.NotFound("User"));
            (await BalanceAsync(Caller.WithNoId("SubscriptionsAdmin_View"), $"?userId={_maria.Id}")).ShouldFail(English.NotSignedIn);
        }


        // ================================================================
        // Putting everybody on nought
        // ================================================================

        private Task<ApiResult<int>> OpenAsync(Caller caller, string? language = null)
        {
            return PostAsync<int>(caller, Url + "/SeedInitialBalances", null, language);
        }

        [Fact]
        public async Task Everybody_is_granted_exactly_what_they_have_used()
        {
            DateTime before = DateTime.UtcNow;
            for (int i = 0; i < 3; i++)
                Seed.Attendance(_maria, new DateTime(2025, 1, 1 + i));
            Seed.Attendance(_nikos, new DateTime(2025, 1, 1));

            ApiResult<int> result = await OpenAsync(Admin);

            result.ShouldSucceed();
            result.Data.ShouldBe(2);
            List<Subscription> opening = All();
            opening.Count.ShouldBe(2);
            opening.Single(x => x.UserId == _maria.Id).Amount.ShouldBe(3);
            opening.Single(x => x.UserId == _nikos.Id).Amount.ShouldBe(1);
            opening.ShouldAllBe(x => x.Status == SubscriptionStatusEnum.APPROVED && x.RequestedAmount == null && x.AdminComment == English.Of(TranslationKeys.Opening_balance) && x.CreatedBy_Id == _admin.Id.ToString());
            opening.ShouldAllBe(x => x.DecidedOn == x.CreatedOn && x.CreatedOn >= before && x.CreatedOn <= DateTime.UtcNow);
            (await BalanceAsync(Admin, $"?userId={_maria.Id}")).Data.ShouldBe(0);
        }

        // A second press, or a member set up by hand: either way it is meant and is left alone.
        [Fact]
        public async Task Anybody_with_granted_lessons_is_left_alone()
        {
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));
            Seed.Attendance(_nikos, new DateTime(2025, 1, 1));
            Seed.Subscription(_maria, 5);
            Seed.Subscription(_nikos, 5, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_nikos, 5, SubscriptionStatusEnum.REJECTED);

            (await OpenAsync(Admin)).Data.ShouldBe(1);
            All().Count.ShouldBe(4);
            (await OpenAsync(Admin)).Data.ShouldBe(0);
            All().Count.ShouldBe(4);
        }

        // Which is not told by the comment: that is in whatever language it was run in.
        [Fact]
        public async Task Another_language_does_not_do_it_again()
        {
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            (await OpenAsync(Admin, "el")).Data.ShouldBe(1);
            All().Single().AdminComment.ShouldBe(Language.Text("el", TranslationKeys.Opening_balance));
            (await OpenAsync(Admin, "en")).Data.ShouldBe(0);
        }

        [Fact]
        public async Task With_no_attendance_there_is_nothing_to_do()
        {
            ApiResult<int> result = await OpenAsync(Admin);

            result.ShouldSucceed();
            result.Data.ShouldBe(0);
            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task It_needs_the_admins_add_permission()
        {
            Seed.Attendance(_maria, new DateTime(2025, 1, 1));

            (await OpenAsync(Without("SubscriptionsAdmin_Add"))).ShouldFail(English.NotAuthorized);
            All().ShouldBeEmpty();

            (await OpenAsync(Caller.WithNoId("SubscriptionsAdmin_Add"))).Data.ShouldBe(1);
            All().Single().CreatedBy_Id.ShouldBe("");
        }


        // ================================================================
        // Deleting
        // ================================================================

        private Task<ApiResult<Subscription>> DeleteAsync(Caller caller, object id)
        {
            return DeleteAsync<Subscription>(caller, $"{Url}/{id}");
        }

        [Theory]
        [InlineData(SubscriptionStatusEnum.PENDING)]
        [InlineData(SubscriptionStatusEnum.APPROVED)]
        [InlineData(SubscriptionStatusEnum.REJECTED)]
        public async Task An_admin_deletes_any_of_them(SubscriptionStatusEnum status)
        {
            Subscription subscription = Seed.Subscription(_maria, 5, status);

            (await DeleteAsync(Admin, subscription.Id)).ShouldSucceed(English.Deleted("Subscription"));

            All().ShouldBeEmpty();
        }

        // Both permissions: the admin's own, and the one anybody needs to delete.
        [Fact]
        public async Task An_admin_needs_both_permissions()
        {
            Subscription subscription = Seed.Subscription(_maria, 5);

            (await DeleteAsync(Without("Subscriptions_Delete"), subscription.Id)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync(Without("SubscriptionsAdmin_Delete"), subscription.Id)).ShouldFail(English.NotAuthorized);

            All().Count.ShouldBe(1);
            (await DeleteAsync(Caller.Of(_admin, "Subscriptions_Delete", "SubscriptionsAdmin_Delete"), subscription.Id)).ShouldSucceed(English.Deleted("Subscription"));
        }

        // Their own, and only while nobody has acted on it.
        [Theory]
        [InlineData(SubscriptionStatusEnum.PENDING, true)]
        [InlineData(SubscriptionStatusEnum.APPROVED, false)]
        [InlineData(SubscriptionStatusEnum.REJECTED, false)]
        public async Task A_member_takes_back_their_own_request(SubscriptionStatusEnum status, bool isAllowed)
        {
            Subscription mine = Seed.Subscription(_maria, 0, status);
            Subscription theirs = Seed.Subscription(_nikos, 0, status);

            (await DeleteAsync(Member, theirs.Id)).ShouldFail(English.NotAuthorized);
            ApiResult<Subscription> result = await DeleteAsync(Member, mine.Id);

            if (isAllowed)
                result.ShouldSucceed(English.Deleted("Subscription"));
            else
                result.ShouldFail(English.NotAuthorized);

            All().Select(x => x.Id).ShouldBe(isAllowed ? [theirs.Id] : [mine.Id, theirs.Id]);
        }

        [Fact]
        public async Task A_member_needs_the_delete_permission_and_an_id()
        {
            Subscription mine = Pending();

            (await DeleteAsync(Caller.Of(_maria, "Subscriptions_View", "Subscriptions_Add"), mine.Id)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync(Caller.WithNoId("Subscriptions_Delete"), mine.Id)).ShouldFail(English.NotAuthorized);

            All().Count.ShouldBe(1);
        }

        // Said before anybody is asked who they are.
        [Theory]
        [InlineData("99")]
        [InlineData("x")]
        [InlineData("01")]
        public async Task One_that_is_not_there_is_not_found(string id)
        {
            Seed.Subscription(_maria, 5);

            (await DeleteAsync(Admin, id)).ShouldFail(English.NotFound("Subscription"));
            (await DeleteAsync(Caller.Of(_nikos), id)).ShouldFail(English.NotFound("Subscription"));
        }


        // ================================================================
        // By id
        // ================================================================

        [Fact]
        public async Task One_is_read_by_its_id_with_the_view_permission()
        {
            Subscription subscription = Pending();

            SubscriptionDto dto = (await GetAsync<SubscriptionDto>(Admin, $"{Url}/{subscription.Id}")).Value;

            dto.Id.ShouldBe(subscription.Id);
            dto.RequestedAmount.ShouldBe(10);
            dto.MemberComment.ShouldBe("Please");
            dto.UserId.ShouldBe(_maria.Id.ToString());
            (await GetAsync<SubscriptionDto>(Without("Subscriptions_View"), $"{Url}/{subscription.Id}")).ShouldFail(English.NotAuthorized);
            (await GetAsync<SubscriptionDto>(Admin, $"{Url}/99")).ShouldFail(English.NotFound("Subscription"));
        }

        [Fact]
        public async Task A_member_reads_their_own()
        {
            Subscription own = Seed.Subscription(_maria, 5, change: x => x.AdminComment = "Well done");

            (await GetAsync<SubscriptionDto>(Member, $"{Url}/{own.Id}")).Value.AdminComment.ShouldBe("Well done");
            (await GetAsync<SubscriptionDto>(Caller.Of(_maria, "Subscriptions_View"), $"{Url}/{own.Id}")).ShouldSucceed();
        }

        // As in the grid.
        [Fact]
        public async Task Somebody_elses_is_answered_like_one_that_is_not_there()
        {
            Subscription theirs = Seed.Subscription(_nikos, 5, change: x => x.AdminComment = "private");

            foreach (Caller caller in new[]
            {
                Member,
                Caller.Of(_maria, "Subscriptions_View", "SubscriptionsAdmin_Add", "SubscriptionsAdmin_Edit", "SubscriptionsAdmin_Delete"),
                Caller.WithNoId("Subscriptions_View")
            })
            {
                ApiResult<SubscriptionDto> result = await GetAsync<SubscriptionDto>(caller, $"{Url}/{theirs.Id}");

                result.ShouldFail(English.NotFound("Subscription"));
                result.Body.ShouldNotContain("private");
            }

            (await GetAsync<SubscriptionDto>(Member, $"{Url}/99")).ShouldFail(English.NotFound("Subscription"));
        }

        [Fact]
        public async Task Reading_an_id_that_is_no_number_breaks()
        {
            (await GetAsync<SubscriptionDto>(Member, $"{Url}/x")).ShouldBreak();
        }

        [Fact]
        public async Task An_admin_reads_anybodys()
        {
            Subscription theirs = Seed.Subscription(_nikos, 5, change: x => x.AdminComment = "private");

            (await GetAsync<SubscriptionDto>(Caller.Of(_admin, "Subscriptions_View", "SubscriptionsAdmin_View"), $"{Url}/{theirs.Id}")).Value.AdminComment.ShouldBe("private");
            (await GetAsync<SubscriptionDto>(Caller.Of(_admin, "SubscriptionsAdmin_View"), $"{Url}/{theirs.Id}")).ShouldFail(English.NotAuthorized);
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<SubscriptionDto>> GridAsync(Caller caller, DataTableDto<SubscriptionDto> grid)
        {
            ApiResult<DataTableDto<SubscriptionDto>> result = await PostAsync<DataTableDto<SubscriptionDto>>(caller, Url + "/GetDataTable", grid);

            result.ShouldSucceed();

            return result.Value;
        }

        private async Task<string> StatusesAsync(params DataTableFilterDto[] filters)
        {
            DataTableDto<SubscriptionDto> grid = await GridAsync(Admin, Grid<SubscriptionDto>(filters));
            grid.TotalRecords.ShouldBe(grid.Data!.Count());

            return string.Join(" ", grid.Data!.OrderBy(x => x.Id).Select(x => x.Status));
        }

        [Fact]
        public async Task An_admin_sees_everybodys_and_a_member_their_own()
        {
            Subscription maria = Seed.Subscription(_maria, 5);
            Subscription nikos = Seed.Subscription(_nikos, 5);

            (await GridAsync(Admin, Grid<SubscriptionDto>())).Data!.Select(x => x.Id).ShouldBe([maria.Id, nikos.Id], ignoreOrder: true);

            DataTableDto<SubscriptionDto> own = await GridAsync(Member, Grid<SubscriptionDto>());
            own.Data!.Single().Id.ShouldBe(maria.Id);
            own.Data!.Single().User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");
            own.TotalRecords.ShouldBe(1);

            (await GridAsync(Member, Grid<SubscriptionDto>(Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals)))).TotalRecords.ShouldBe(0);
            (await GridAsync(Caller.WithNoId("Subscriptions_View"), Grid<SubscriptionDto>())).TotalRecords.ShouldBe(0);
        }

        // By its name in any case, or by its number; whatever kind of filter it is sent as. One that is
        // no status is as if it had not been sent.
        [Theory]
        [InlineData("PENDING", "PENDING")]
        [InlineData("pending", "PENDING")]
        [InlineData("APPROVED", "APPROVED")]
        [InlineData("REJECTED", "REJECTED")]
        [InlineData("1", "APPROVED")]
        [InlineData("7", "")]
        [InlineData("null", "PENDING APPROVED REJECTED")]
        [InlineData("other", "PENDING APPROVED REJECTED")]
        [InlineData("", "PENDING APPROVED REJECTED")]
        [InlineData(null, "PENDING APPROVED REJECTED")]
        public async Task It_filters_by_status(string? value, string expected)
        {
            foreach (SubscriptionStatusEnum status in Enum.GetValues<SubscriptionStatusEnum>())
                Seed.Subscription(_maria, 5, status);

            foreach (string field in new[] { "status", "Status", "STATUS" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.equals, DataTableFiltersEnum.contains, DataTableFiltersEnum.custom, DataTableFiltersEnum.notEquals })
                    (await StatusesAsync(Filter(field, value, type))).ShouldBe(expected, $"{field} {type}");
        }

        [Fact]
        public async Task Only_the_first_of_two_status_filters_counts()
        {
            foreach (SubscriptionStatusEnum status in Enum.GetValues<SubscriptionStatusEnum>())
                Seed.Subscription(_maria, 5, status);

            (await StatusesAsync(Filter("status", "PENDING"), Filter("status", "REJECTED"))).ShouldBe("PENDING");
        }

        [Fact]
        public async Task It_filters_by_the_member_as_well()
        {
            Seed.Subscription(_maria, 1, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_maria, 2);
            Seed.Subscription(_nikos, 3, SubscriptionStatusEnum.PENDING);

            DataTableDto<SubscriptionDto> grid = await GridAsync(Admin, Grid<SubscriptionDto>(Filter("status", "PENDING"), Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals)));

            grid.Data!.Single().Amount.ShouldBe(3);
            grid.TotalRecords.ShouldBe(1);
        }

        // Pending, approved, rejected; a nought counts as down.
        [Theory]
        [InlineData(1, "PENDING APPROVED REJECTED")]
        [InlineData(5, "PENDING APPROVED REJECTED")]
        [InlineData(-1, "REJECTED APPROVED PENDING")]
        [InlineData(0, "REJECTED APPROVED PENDING")]
        public async Task It_sorts_by_status(int order, string expected)
        {
            Seed.Subscription(_maria, 5, SubscriptionStatusEnum.APPROVED);
            Seed.Subscription(_maria, 5, SubscriptionStatusEnum.REJECTED);
            Seed.Subscription(_maria, 5, SubscriptionStatusEnum.PENDING);

            foreach (string field in new[] { "status", "Status" })
                string.Join(" ", (await GridAsync(Admin, SortedGrid<SubscriptionDto>((field, order)))).Data!.Select(x => x.Status)).ShouldBe(expected);
        }

        [Fact]
        public async Task It_sorts_by_any_other_column()
        {
            Seed.Subscription(_maria, 5);
            Seed.Subscription(_maria, 9);
            Seed.Subscription(_maria, 1);

            (await GridAsync(Admin, SortedGrid<SubscriptionDto>(("amount", 1)))).Data!.Select(x => x.Amount).ShouldBe([1, 5, 9]);
            (await GridAsync(Admin, SortedGrid<SubscriptionDto>(("amount", -1)))).Data!.Select(x => x.Amount).ShouldBe([9, 5, 1]);
        }

        // The other column takes over, whichever of the two was asked for first.
        [Fact]
        public async Task Status_beside_another_sort_counts_for_nothing()
        {
            Seed.Subscription(_maria, 1, SubscriptionStatusEnum.REJECTED);
            Seed.Subscription(_maria, 9, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_maria, 5, SubscriptionStatusEnum.APPROVED);

            (await GridAsync(Admin, SortedGrid<SubscriptionDto>(("status", 1), ("amount", 1)))).Data!.Select(x => x.Amount).ShouldBe([1, 5, 9]);
            (await GridAsync(Admin, SortedGrid<SubscriptionDto>(("amount", 1), ("status", 1)))).Data!.Select(x => x.Amount).ShouldBe([1, 5, 9]);
        }
    }
}
