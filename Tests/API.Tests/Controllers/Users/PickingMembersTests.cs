using API.Controllers;
using API.Tests.Host;
using Core.Dtos.AutoComplete;
using Core.Dtos.Lookup;
using Core.Dtos.User;
using Core.Models;

namespace API.Tests.Controllers.Users
{
    /// <summary>
    /// Finding a member by what is typed: in the dropdowns and the filters, and in the box a mail is
    /// addressed in. Staff find anybody, and a member finds themselves.
    /// </summary>
    public sealed class PickingMembersTests : ApiTest
    {
        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _yannis;

        public PickingMembersTests(ApiFixture api) : base(api)
        {
            UserStatus active = Seed.UserStatus(x => x.Color = "#00ff00");
            _maria = Seed.User(x => { x.UserName = "mar1"; x.FirstName = "Μαρία"; x.LastName = "Παπά"; x.Email = "maria@gym.test"; x.UserStatusId = active.Id; x.ProfileImage = [1, 2]; x.MedicalHistory = "private"; x.Address = "street"; });
            _nikos = Seed.User(x => { x.UserName = "nik2"; x.FirstName = "Νίκος"; x.LastName = "Παπάς"; x.Email = "nikos@gym.test"; });
            _yannis = Seed.User(x => { x.UserName = "yan3"; x.FirstName = "Γιάννης"; x.LastName = "Άλφα"; x.Email = "yannis@gym.test"; });
            Seed.PhoneNumber(_yannis, "2109876543");
            Seed.PhoneNumber(_yannis, "6971112223");
        }

        private Task<ApiResult<LookupDto>> LookupAsync(Caller caller, string value = "", string id = "", int skip = 0, int take = 50)
        {
            return PostAsync<LookupDto>(caller, "api/Users/Lookup", new { skip, take, filter = new { id, value } });
        }

        private Task<ApiResult<AutoCompleteDto<UserDto>>> SuggestAsync(Caller caller, string value = "", int skip = 0, int take = 50)
        {
            return PostAsync<AutoCompleteDto<UserDto>>(caller, "api/Users/AutoComplete", new { searchValue = value, skip, take });
        }

        // Who is found, the same in the one as in the other.
        private async Task<string> FoundAsync(Caller caller, string value)
        {
            LookupDto lookup = (await LookupAsync(caller, value)).Value;
            AutoCompleteDto<UserDto> box = (await SuggestAsync(caller, value)).Value;

            string found = string.Join(" ", lookup.Data.Select(x => x.Value));
            string.Join(" ", box.Suggestions.Select(x => x.UserName)).ShouldBe(found);
            lookup.TotalRecords.ShouldBe(lookup.Data.Count);
            box.TotalRecords.ShouldBe(box.Suggestions.Count);

            return found;
        }


        // ================================================================
        // Who may look for whom
        // ================================================================

        [Theory]
        [InlineData("Users_View", true)]
        [InlineData("TrainGroups_View", true)]
        [InlineData("TrainGroupParticipants_View", true)]
        [InlineData("WorkoutPlansAdmin_View", true)]
        [InlineData("WorkoutPlanRecordingsAdmin_View", true)]
        [InlineData("SubscriptionsAdmin_View", true)]
        [InlineData("SubscriptionsAdmin_Edit", true)]
        [InlineData("Mails_View", true)]
        [InlineData("Mails_Add", true)]
        // What a member has, and some that are close.
        [InlineData("WorkoutPlans_View", false)]
        [InlineData("WorkoutPlans_Edit", false)]
        [InlineData("TrainGroupParticipants_Delete", false)]
        [InlineData("WorkoutPlanRecordings_View", false)]
        [InlineData("WorkoutPlanRecordings_Add", false)]
        [InlineData("Subscriptions_View", false)]
        [InlineData("Subscriptions_Add", false)]
        [InlineData("Subscriptions_Delete", false)]
        [InlineData("Users_Edit", false)]
        [InlineData("SubscriptionsAdmin_Add", false)]
        [InlineData("users_view", false)]
        public async Task Staff_find_anybody_and_a_member_themselves(string permission, bool isStaff)
        {
            (await FoundAsync(Caller.Of(_nikos, permission), "")).ShouldBe(isStaff ? "yan3 mar1 nik2" : "nik2");
            (await FoundAsync(Caller.Of(_nikos, permission), "mar1")).ShouldBe(isStaff ? "mar1" : "");
        }

        [Fact]
        public async Task A_token_that_does_not_say_who_finds_nobody()
        {
            (await FoundAsync(Caller.WithNoId(), "")).ShouldBe("");
            (await FoundAsync(Caller.WithNoId("Users_View"), "")).ShouldBe("yan3 mar1 nik2");
        }


        // ================================================================
        // What is typed
        // ================================================================

        [Theory]
        // The first name, the last, the user name, the address; without accents and in any case.
        [InlineData("μαρια", "mar1")]
        [InlineData("ΜΑΡΊΑ", "mar1")]
        [InlineData("παπ", "mar1 nik2")]
        [InlineData("YAN", "yan3")]
        [InlineData("gym.test", "yan3 mar1 nik2")]
        [InlineData("nikos@", "nik2")]
        // A number, as it is written.
        [InlineData("2109876543", "yan3")]
        [InlineData("697", "yan3")]
        [InlineData("210 697", "yan3")]
        // Every word has to be somewhere.
        [InlineData("νίκος παπ", "nik2")]
        [InlineData("παπ νίκος", "nik2")]
        [InlineData("νίκος μαρία", "")]
        [InlineData("γιάννης 2109876543", "yan3")]
        // Nothing at all, and nothing but spaces and brackets.
        [InlineData("", "yan3 mar1 nik2")]
        [InlineData("   ", "yan3 mar1 nik2")]
        [InlineData("()", "yan3 mar1 nik2")]
        [InlineData("( , )", "yan3 mar1 nik2")]
        // As the dropdown writes a member.
        [InlineData("Νίκος Παπάς (nik2)", "nik2")]
        [InlineData("  Παπάς,   (nik2)  ", "nik2")]
        [InlineData("none", "")]
        [InlineData("%", "")]
        public async Task Members_are_found_by_every_word_of_it(string value, string expected)
        {
            (await FoundAsync(Caller.Of(_nikos, "Users_View"), value)).ShouldBe(expected);
        }

        // Tabs and line ends come between words as spaces do.
        [Fact]
        public async Task Words_are_what_is_between_white_space()
        {
            (await FoundAsync(Caller.Of(_nikos, "Users_View"), "νίκος\tπαπ\n")).ShouldBe("nik2");
        }


        // ================================================================
        // The dropdown
        // ================================================================

        [Fact]
        public async Task An_option_is_a_name_a_face_and_a_colour()
        {
            ApiResult<LookupDto> result = await LookupAsync(Caller.Of(_nikos, "Users_View"), "mar1");

            LookupOptionDto option = result.Value.Data.Single();
            option.Id.ShouldBe(_maria.Id.ToString());
            option.Value.ShouldBe("mar1");
            option.FirstName.ShouldBe("Μαρία");
            option.LastName.ShouldBe("Παπά");
            option.ProfileImage.ShouldBe([1, 2]);
            option.UserColor.ShouldBe("#00ff00");
            result.Body.ShouldNotContain("maria@gym.test");
            result.Body.ShouldNotContain("private");
            result.Body.ShouldNotContain("street");

            (await LookupAsync(Caller.Of(_nikos, "Users_View"), "nik2")).Value.Data.Single().UserColor.ShouldBeNullOrEmpty();
        }

        [Fact]
        public async Task The_one_already_chosen_is_found_by_id()
        {
            Caller staff = Caller.Of(_nikos, "Users_View");

            (await LookupAsync(staff, id: _maria.Id.ToString())).Value.Data.Single().Value.ShouldBe("mar1");
            (await LookupAsync(staff, id: _maria.Id.ToString().ToUpperInvariant())).Value.Data.Single().Value.ShouldBe("mar1");
            (await LookupAsync(staff, id: Guid.NewGuid().ToString())).Value.Data.ShouldBeEmpty();
            // One that is no id is nobody's.
            (await LookupAsync(staff, id: "x")).Value.Data.ShouldBeEmpty();
            (await LookupAsync(staff, "nik2", _maria.Id.ToString())).Value.Data.ShouldBeEmpty();
            (await LookupAsync(Caller.Of(_nikos), id: _maria.Id.ToString())).Value.Data.ShouldBeEmpty();
            (await LookupAsync(Caller.Of(_nikos), id: _nikos.Id.ToString())).Value.Data.Single().Value.ShouldBe("nik2");
        }

        [Fact]
        public async Task A_dropdown_needs_a_filter()
        {
            (await PostAsync<LookupDto>(Caller.Of(_nikos, "Users_View"), "api/Users/Lookup", new { take = 10 })).ShouldBeBad(English.Required("Filter"));
        }

        // Json that says null is refused before it gets here.
        [Fact]
        public async Task What_is_missing_is_taken_for_empty()
        {
            UsersController controller = Direct.Controller<UsersController>(Services, "Users", ["Users_View"]);

            (await controller.Lookup(new LookupDto { Take = 10, Filter = new LookupFilterDto { Id = null!, Value = null! } })).Data.ShouldNotBeNull().Data.Count.ShouldBe(3);
            (await controller.AutoComplete(new AutoCompleteDto<UserDto> { Take = 10, SearchValue = null! })).Data.ShouldNotBeNull().Suggestions.Count.ShouldBe(3);
        }


        // ================================================================
        // The box a mail is addressed in
        // ================================================================

        // Not the whole record.
        [Fact]
        public async Task A_suggestion_is_a_name_a_face_and_an_address()
        {
            ApiResult<AutoCompleteDto<UserDto>> result = await SuggestAsync(Caller.Of(_nikos, "Mails_Add"), "mar1");

            UserDto suggestion = result.Value.Suggestions.Single();
            suggestion.Id.ShouldBe(_maria.Id.ToString());
            suggestion.UserName.ShouldBe("mar1");
            suggestion.FirstName.ShouldBe("Μαρία");
            suggestion.LastName.ShouldBe("Παπά");
            suggestion.Email.ShouldBe("maria@gym.test");
            suggestion.ProfileImage.ShouldBe([1, 2]);
            suggestion.UserStatusId.ShouldBe(_maria.UserStatusId);
            result.Body.ShouldNotContain("private");
            result.Body.ShouldNotContain("street");
        }


        // ================================================================
        // The order and the pages
        // ================================================================

        // By the first name, then the last, then the user name, and by their letters' numbers.
        [Fact]
        public async Task They_are_in_the_order_of_their_names()
        {
            Seed.User(x => { x.UserName = "c"; x.FirstName = "Anna"; x.LastName = "Beta"; });
            Seed.User(x => { x.UserName = "b"; x.FirstName = "Anna"; x.LastName = "Beta"; });
            Seed.User(x => { x.UserName = "a"; x.FirstName = "Anna"; x.LastName = "Gamma"; });

            (await FoundAsync(Caller.Of(_nikos, "Users_View"), "")).ShouldBe("b c a yan3 mar1 nik2");
        }

        [Theory]
        [InlineData(0, 2, "yan3 mar1")]
        [InlineData(2, 2, "nik2")]
        [InlineData(3, 2, "")]
        [InlineData(0, 0, "")]
        public async Task A_page_counts_them_all(int skip, int take, string expected)
        {
            LookupDto lookup = (await LookupAsync(Caller.Of(_nikos, "Users_View"), skip: skip, take: take)).Value;
            AutoCompleteDto<UserDto> box = (await SuggestAsync(Caller.Of(_nikos, "Users_View"), skip: skip, take: take)).Value;

            string.Join(" ", lookup.Data.Select(x => x.Value)).ShouldBe(expected);
            string.Join(" ", box.Suggestions.Select(x => x.UserName)).ShouldBe(expected);
            lookup.TotalRecords.ShouldBe(3);
            box.TotalRecords.ShouldBe(3);
        }
    }
}
