using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.PhoneNumber;
using Core.Enums;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers
{
    /// <summary>
    /// A member's numbers, one of which is the primary one. A member has the say over their own with
    /// no permission at all, and over anybody else's with the one to view or to edit members.
    /// </summary>
    public sealed class PhoneNumbersControllerTests : ApiTest
    {
        private const string Url = "api/PhoneNumbers";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly PhoneNumber _primary;
        private readonly PhoneNumber _second;
        private readonly Caller _caller;

        public PhoneNumbersControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User();
            _nikos = Seed.User();
            _primary = Seed.PhoneNumber(_maria, "2101111111", true);
            _second = Seed.PhoneNumber(_maria, "2102222222");
            Seed.PhoneNumber(_nikos, "2103333333", true);
            _caller = Caller.Of(_maria);
        }

        private List<string> NumbersOf(User user)
        {
            return Database.Read(db => db.PhoneNumbers.Where(x => x.UserId == user.Id).OrderBy(x => x.Id).Select(x => x.Number + (x.IsPrimary ? " primary" : "")).ToList());
        }

        private Task<ApiResult<PhoneNumber>> EditAsync(Caller caller, int? id, string number, bool isPrimary, User user)
        {
            return PutAsync<PhoneNumber>(caller, $"{Url}/{id}", new { id, number, isPrimary, userId = user.Id });
        }


        // ================================================================
        // Editing
        // ================================================================

        [Fact]
        public async Task A_number_is_changed()
        {
            ApiResult<PhoneNumber> result = await EditAsync(_caller, _second.Id, "2109999999", false, _maria);

            result.ShouldSucceed();
            result.Value.Number.ShouldBe("2109999999");
            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2109999999"]);
        }

        [Fact]
        public async Task The_primary_number_is_changed_and_stays_the_primary_one()
        {
            (await EditAsync(_caller, _primary.Id, "2109999999", true, _maria)).ShouldSucceed();

            NumbersOf(_maria).ShouldBe(["2109999999 primary", "2102222222"]);
        }

        [Fact]
        public async Task A_number_made_the_primary_one_takes_it_from_the_other()
        {
            (await EditAsync(_caller, _second.Id, "2102222222", true, _maria)).ShouldSucceed();

            NumbersOf(_maria).ShouldBe(["2101111111", "2102222222 primary"]);
            // And from nobody else.
            NumbersOf(_nikos).ShouldBe(["2103333333 primary"]);
        }

        [Fact]
        public async Task The_only_primary_number_cannot_stop_being_it()
        {
            (await EditAsync(_caller, _primary.Id, "2109999999", false, _maria)).ShouldFail(English.Of(TranslationKeys.At_least_1_primary_phone_number_is_required));

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222"]);
        }

        // Two of them should not be there, but then one may be given up.
        [Fact]
        public async Task One_of_two_primary_numbers_can_stop_being_it()
        {
            Database.Change(db => db.PhoneNumbers.Single(x => x.Id == _second.Id).IsPrimary = true);

            (await EditAsync(_caller, _primary.Id, "2101111111", false, _maria)).ShouldSucceed();

            NumbersOf(_maria).ShouldBe(["2101111111", "2102222222 primary"]);
        }

        [Fact]
        public async Task With_no_primary_number_at_all_the_first_to_be_edited_has_to_become_it()
        {
            Database.Change(db => db.PhoneNumbers.Single(x => x.Id == _primary.Id).IsPrimary = false);

            (await EditAsync(_caller, _second.Id, "2102222222", false, _maria)).ShouldFail(English.Of(TranslationKeys.At_least_1_primary_phone_number_is_required));
            (await EditAsync(_caller, _second.Id, "2102222222", true, _maria)).ShouldSucceed();
        }

        // Looked for among the numbers of the member the form names.
        [Fact]
        public async Task A_number_that_is_not_the_members_is_not_found()
        {
            (await EditAsync(Caller.Of(_nikos), _second.Id, "2109999999", true, _nikos)).ShouldFail(English.NotFound("PhoneNumber"));
            (await EditAsync(Caller.Of(_nikos, "Users_Edit"), _second.Id, "2109999999", true, _nikos)).ShouldFail(English.NotFound("PhoneNumber"));
            (await EditAsync(_caller, 99, "2109999999", false, _maria)).ShouldFail(English.NotFound("PhoneNumber"));

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222"]);
            NumbersOf(_nikos).ShouldBe(["2103333333 primary"]);
        }

        // The id in the body is the one; the one in the address is not looked at.
        [Fact]
        public async Task The_id_in_the_address_does_not_matter()
        {
            (await PutAsync<PhoneNumber>(_caller, $"{Url}/not-an-id", new { id = _second.Id, number = "2109999999", isPrimary = false, userId = _maria.Id })).ShouldSucceed();

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2109999999"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Users_View Users_Add Users_Delete")]
        [InlineData("PhoneNumbers_View PhoneNumbers_Add PhoneNumbers_Edit PhoneNumbers_Delete")]
        [InlineData("TrainGroupParticipants_View")]
        public async Task Somebody_elses_number_cannot_be_changed(string permissions)
        {
            Caller caller = Caller.Of(_nikos, permissions.Split(' '));

            (await EditAsync(caller, _second.Id, "2109999999", false, _maria)).ShouldFail(English.NotAuthorized);
            // Nor can it be found out which there are.
            (await EditAsync(caller, 99, "2109999999", false, _maria)).ShouldFail(English.NotAuthorized);
            (await EditAsync(Caller.WithNoId(permissions.Split(' ')), _second.Id, "2109999999", false, _maria)).ShouldFail(English.NotAuthorized);

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222"]);
        }

        [Fact]
        public async Task Staff_who_may_edit_members_change_anybodys()
        {
            (await EditAsync(Caller.Of(_nikos, "Users_Edit"), _second.Id, "2109999999", false, _maria)).ShouldSucceed();

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2109999999"]);
        }

        [Fact]
        public async Task A_number_that_will_not_do_is_refused()
        {
            (await EditAsync(_caller, _second.Id, "not a number", false, _maria)).ShouldBeBad(English.Of(TranslationKeys.Invalid_phone_number_format));
            (await PutAsync<PhoneNumber>(_caller, $"{Url}/{_second.Id}", new { id = _second.Id, number = "2109999999", userId = "" })).ShouldBeBad(English.Required("UserId"));
        }

        [Fact]
        public async Task A_member_who_is_no_id_breaks()
        {
            (await PutAsync<PhoneNumber>(_caller, $"{Url}/{_second.Id}", new { id = _second.Id, number = "2109999999", userId = "x" })).ShouldBreak();
        }


        // ================================================================
        // Deleting
        // ================================================================

        [Fact]
        public async Task A_number_is_deleted_unless_it_is_the_primary_one()
        {
            (await DeleteAsync<PhoneNumber>(_caller, $"{Url}/{_second.Id}")).ShouldSucceed(English.Deleted("PhoneNumber"));
            (await DeleteAsync<PhoneNumber>(_caller, $"{Url}/{_primary.Id}")).ShouldBeBad(English.Of(TranslationKeys.Primary_phone_number_cannot_be_deleted));

            NumbersOf(_maria).ShouldBe(["2101111111 primary"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Users_View Users_Add Users_Delete")]
        [InlineData("PhoneNumbers_View PhoneNumbers_Add PhoneNumbers_Edit PhoneNumbers_Delete")]
        public async Task Somebody_elses_number_cannot_be_deleted(string permissions)
        {
            Caller caller = Caller.Of(_nikos, permissions.Split(' '));

            (await DeleteAsync<PhoneNumber>(caller, $"{Url}/{_second.Id}")).ShouldBeBad(English.NotAuthorized);
            (await DeleteAsync<PhoneNumber>(caller, $"{Url}/{_primary.Id}")).ShouldBeBad(English.NotAuthorized);
            (await DeleteAsync<PhoneNumber>(Caller.WithNoId(permissions.Split(' ')), $"{Url}/{_second.Id}")).ShouldBeBad(English.NotAuthorized);

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222"]);
        }

        [Fact]
        public async Task Staff_who_may_edit_members_delete_anybodys_but_the_primary_one()
        {
            Caller staff = Caller.Of(_nikos, "Users_Edit");

            (await DeleteAsync<PhoneNumber>(staff, $"{Url}/{_primary.Id}")).ShouldBeBad(English.Of(TranslationKeys.Primary_phone_number_cannot_be_deleted));
            (await DeleteAsync<PhoneNumber>(staff, $"{Url}/{_second.Id}")).ShouldSucceed(English.Deleted("PhoneNumber"));

            NumbersOf(_maria).ShouldBe(["2101111111 primary"]);
        }

        [Fact]
        public async Task A_number_that_is_not_there_is_not_found()
        {
            (await DeleteAsync<PhoneNumber>(_caller, $"{Url}/99")).ShouldFail(English.NotFound("PhoneNumber"));
        }


        // ================================================================
        // Reading
        // ================================================================

        [Fact]
        public async Task A_member_reads_their_own_number()
        {
            PhoneNumberDto number = (await GetAsync<PhoneNumberDto>(_caller, $"{Url}/{_second.Id}")).Value;

            number.Id.ShouldBe(_second.Id);
            number.Number.ShouldBe("2102222222");
            number.IsPrimary.ShouldBeFalse();
            number.UserId.ShouldBe(_maria.Id.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("Users_Add Users_Edit Users_Delete")]
        [InlineData("PhoneNumbers_View")]
        [InlineData("users_view")]
        public async Task Somebody_elses_is_answered_like_one_that_is_not_there(string permissions)
        {
            (await GetAsync<PhoneNumberDto>(Caller.Of(_nikos, permissions.Split(' ')), $"{Url}/{_second.Id}")).ShouldFail(English.NotFound("PhoneNumber"));
            (await GetAsync<PhoneNumberDto>(Caller.WithNoId(permissions.Split(' ')), $"{Url}/{_second.Id}")).ShouldFail(English.NotFound("PhoneNumber"));
            (await GetAsync<PhoneNumberDto>(_caller, $"{Url}/99")).ShouldFail(English.NotFound("PhoneNumber"));
        }

        [Fact]
        public async Task An_id_that_is_no_number_breaks()
        {
            (await GetAsync<PhoneNumberDto>(_caller, $"{Url}/x")).ShouldBreak();
        }

        [Fact]
        public async Task Staff_who_may_view_members_read_anybodys()
        {
            (await GetAsync<PhoneNumberDto>(Caller.Of(_nikos, "Users_View"), $"{Url}/{_second.Id}")).Value.Number.ShouldBe("2102222222");
            (await GetAsync<PhoneNumberDto>(Caller.WithNoId("Users_View"), $"{Url}/{_second.Id}")).Value.Number.ShouldBe("2102222222");
        }


        // ================================================================
        // Adding
        // ================================================================

        private Task<ApiResult<List<PhoneNumber>>> AddAsync(Caller caller, params User[] owners)
        {
            return PostAsync<List<PhoneNumber>>(caller, Url, owners.Select(x => new { number = "2104444444", isPrimary = true, userId = x.Id }));
        }

        [Fact]
        public async Task A_member_adds_a_number_of_their_own()
        {
            (await AddAsync(_caller, _maria)).ShouldSucceed(English.Updated("PhoneNumber"));

            // A second primary one is not asked about.
            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222", "2104444444 primary"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Users_View Users_Add Users_Delete")]
        [InlineData("PhoneNumbers_View PhoneNumbers_Add PhoneNumbers_Edit PhoneNumbers_Delete")]
        public async Task Nobody_is_given_a_number_by_somebody_else(string permissions)
        {
            Caller caller = Caller.Of(_nikos, permissions.Split(' '));

            (await AddAsync(caller, _maria)).ShouldBeBad(English.NotAuthorized);
            (await AddAsync(Caller.WithNoId(permissions.Split(' ')), _maria)).ShouldBeBad(English.NotAuthorized);
            // Nor beside one of their own.
            (await AddAsync(caller, _nikos, _maria)).ShouldBeBad(English.NotAuthorized);

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222"]);
            NumbersOf(_nikos).ShouldBe(["2103333333 primary"]);
        }

        [Fact]
        public async Task Staff_who_may_edit_members_add_one_to_anybody()
        {
            (await AddAsync(Caller.Of(_nikos, "Users_Edit"), _maria)).ShouldSucceed(English.Updated("PhoneNumber"));

            NumbersOf(_maria).ShouldBe(["2101111111 primary", "2102222222", "2104444444 primary"]);
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<string> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            DataTableDto<PhoneNumberDto> answer = (await PostAsync<DataTableDto<PhoneNumberDto>>(caller, Url + "/GetDataTable", Grid<PhoneNumberDto>(filters))).Value;

            answer.TotalRecords.ShouldBe(answer.Data!.Count());

            return string.Join(" ", answer.Data!.Select(x => x.Number));
        }

        [Fact]
        public async Task The_grid_gives_a_member_their_own_and_staff_everybodys()
        {
            (await GridAsync(_caller)).ShouldBe("2101111111 2102222222");
            (await GridAsync(Caller.Of(_nikos))).ShouldBe("2103333333");
            (await GridAsync(Caller.Of(_nikos, "Users_Edit", "TrainGroupParticipants_View"))).ShouldBe("2103333333");
            (await GridAsync(Caller.Of(_nikos, "Users_View"))).ShouldBe("2101111111 2102222222 2103333333");
            (await GridAsync(Caller.WithNoId())).ShouldBe("");
        }

        [Fact]
        public async Task A_filter_does_not_open_somebody_elses()
        {
            (await GridAsync(_caller, Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("");
            (await GridAsync(Caller.Of(_maria, "Users_View"), Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("2103333333");
        }
    }
}
