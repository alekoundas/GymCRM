using System.Net;
using API.Tests.Host;
using Business.Repository;
using Core.Dtos.UserStatus;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// What most controllers inherit: reading, adding, editing and deleting a row. Through the
    /// member statuses, which change nothing about it.
    /// </summary>
    public sealed class GenericControllerTests : ApiTest
    {
        private const string Url = "api/UserStatuses";

        private readonly User _user;
        private readonly UserStatus _active;
        private readonly UserStatus _gone;

        public GenericControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _active = Seed.UserStatus(x => { x.Name = "Active"; x.Color = "#00ff00"; x.CreatedBy_Id = "somebody"; x.CreatedOn = new DateTime(2031, 3, 5); });
            _gone = Seed.UserStatus(x => { x.Name = "Gone"; x.Color = "#ff0000"; });
        }

        private Caller With(params string[] permissions)
        {
            return Caller.Of(_user, permissions);
        }

        // Every permission on the statuses but the one, and the one on something else.
        private Caller Without(string action)
        {
            string[] others = new[] { "View", "Add", "Edit", "Delete" }.Where(x => x != action).Select(x => "UserStatuses_" + x).ToArray();

            return Caller.Of(_user, [.. others, "Mails_" + action, "UserStatuses_", "userstatuses_" + action.ToLowerInvariant()]);
        }

        private List<string> Statuses()
        {
            return Database.Read(db => db.UserStatuses.OrderBy(x => x.Id).Select(x => $"{x.Name} {x.Color}").ToList());
        }


        // ================================================================
        // Reading one
        // ================================================================

        [Fact]
        public async Task One_row_is_read_by_its_id()
        {
            ApiResult<UserStatusDto> result = await GetAsync<UserStatusDto>(With("UserStatuses_View"), $"{Url}/{_active.Id}");

            result.ShouldSucceed();
            result.Value.Id.ShouldBe(_active.Id);
            result.Value.Name.ShouldBe("Active");
            result.Value.Color.ShouldBe("#00ff00");
        }

        [Fact]
        public async Task Reading_needs_the_permission_to_view()
        {
            (await GetAsync<UserStatusDto>(Without("View"), $"{Url}/{_active.Id}")).ShouldFail(English.NotAuthorized);
            (await GetAsync<UserStatusDto>(With(), $"{Url}/{_active.Id}")).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task A_row_that_is_not_there_is_not_found()
        {
            (await GetAsync<UserStatusDto>(With("UserStatuses_View"), $"{Url}/99")).ShouldFail(English.NotFound("UserStatus"));
            (await GetAsync<UserStatusDto>(With("UserStatuses_View"), $"{Url}/-1")).ShouldFail(English.NotFound("UserStatus"));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public async Task An_id_that_is_not_a_number_breaks(string id)
        {
            (await GetAsync<UserStatusDto>(With("UserStatuses_View"), $"{Url}/{id}")).ShouldBreak();
        }


        // ================================================================
        // Adding
        // ================================================================

        [Fact]
        public async Task Rows_are_added_as_they_were_sent()
        {
            object[] rows = [new { name = "New", color = "#0000ff" }, new { name = "Newer", color = "blue", id = 77, createdBy_Id = "me" }];

            ApiResult<List<UserStatus>> result = await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, rows);

            // The message is the one for an update.
            result.ShouldSucceed(English.Updated("UserStatus"));
            result.Value.Select(x => x.Name).ShouldBe(["New", "Newer"]);
            result.Value.ShouldAllBe(x => x.Id > _gone.Id && x.Id != 77);
            Statuses().ShouldBe(["Active #00ff00", "Gone #ff0000", "New #0000ff", "Newer blue"]);
            Database.Read(db => db.UserStatuses.Where(x => x.CreatedBy_Id == "me").Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Adding_needs_the_permission_to_add()
        {
            (await PostAsync<List<UserStatus>>(Without("Add"), Url, new[] { new { name = "New", color = "x" } })).ShouldFail(English.NotAuthorized);

            Statuses().Count.ShouldBe(2);
        }

        [Fact]
        public async Task Adding_none_is_an_error()
        {
            (await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, Array.Empty<object>()))
                .ShouldFail(English.Of(TranslationKeys.An_error_occurred_while_creating_the_entity));
        }

        // Checked before the permission is, and all of them before any is written.
        [Fact]
        public async Task Adding_one_that_will_not_do_adds_none()
        {
            object[] rows = [new { name = "New", color = "x" }, new { name = "", color = " " }];

            (await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, rows)).ShouldBeBad(English.Required("Name"), English.Required("Color"));
            (await PostAsync<List<UserStatus>>(With(), Url, rows)).Status.ShouldBe(HttpStatusCode.BadRequest);

            Statuses().Count.ShouldBe(2);
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        public async Task Nothing_sent_at_all_is_refused(string body)
        {
            StringContent content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            (await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, content))
                .ShouldBeBad("A non-empty request body is required.", "The entityDtos field is required.");
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[1]")]
        [InlineData("[")]
        [InlineData("text")]
        public async Task Something_that_is_not_a_list_of_rows_is_refused(string body)
        {
            StringContent content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            ApiResult<List<UserStatus>> result = await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, content);

            result.Status.ShouldBe(HttpStatusCode.BadRequest);
            result.IsSucceed.ShouldBeFalse();
            Statuses().Count.ShouldBe(2);
        }

        [Fact]
        public async Task Something_that_is_not_json_is_not_taken()
        {
            StringContent content = new StringContent("name=New", System.Text.Encoding.UTF8, "text/plain");

            (await PostAsync<List<UserStatus>>(With("UserStatuses_Add"), Url, content)).Status.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        }


        // ================================================================
        // Editing
        // ================================================================

        [Fact]
        public async Task A_row_is_written_as_it_was_sent()
        {
            ApiResult<UserStatus> result = await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { id = _active.Id, name = "Changed", color = "#123456" });

            result.ShouldSucceed(English.Updated("UserStatus"));
            result.Value.Name.ShouldBe("Changed");
            Statuses().ShouldBe(["Changed #123456", "Gone #ff0000"]);
        }

        // Every column is written, and the ones the form does not send start over: who made the row
        // is lost and it reads as made just now.
        [Fact]
        public async Task An_edit_forgets_who_made_the_row_and_when()
        {
            DateTime before = DateTime.UtcNow;

            await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { id = _active.Id, name = "Changed", color = "x" });

            UserStatus status = Database.Read(db => db.UserStatuses.Single(x => x.Id == _active.Id));
            status.CreatedBy_Id.ShouldBe("");
            status.CreatedOn.ShouldBeInRange(before, DateTime.UtcNow);
        }

        // The id in the address is only looked up; the one in the body is written to.
        [Fact]
        public async Task The_id_in_the_body_is_the_one_that_is_written()
        {
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { id = _gone.Id, name = "Changed", color = "x" })).ShouldSucceed(English.Updated("UserStatus"));

            Statuses().ShouldBe(["Active #00ff00", "Changed x"]);
        }

        [Fact]
        public async Task Editing_needs_the_permission_to_edit()
        {
            (await PutAsync<UserStatus>(Without("Edit"), $"{Url}/{_active.Id}", new { id = _active.Id, name = "Changed", color = "x" })).ShouldFail(English.NotAuthorized);

            Statuses().ShouldBe(["Active #00ff00", "Gone #ff0000"]);
        }

        [Fact]
        public async Task A_row_that_is_not_there_cannot_be_edited()
        {
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/99", new { id = _active.Id, name = "Changed", color = "x" })).ShouldFail(English.NotFound("UserStatus"));

            Statuses().ShouldBe(["Active #00ff00", "Gone #ff0000"]);
        }

        [Fact]
        public async Task An_edit_that_will_not_do_changes_nothing()
        {
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { id = _active.Id, name = "", color = "x" })).ShouldBeBad(English.Required("Name"));

            Statuses().ShouldBe(["Active #00ff00", "Gone #ff0000"]);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("99999999999")]
        public async Task An_id_that_is_not_a_number_breaks_an_edit(string id)
        {
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{id}", new { id = _active.Id, name = "Changed", color = "x" })).ShouldBreak();
        }

        // The body names a row that is not there, or none at all.
        [Fact]
        public async Task A_body_with_the_wrong_id_breaks()
        {
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { id = 99, name = "Changed", color = "x" })).ShouldBreak();
            (await PutAsync<UserStatus>(With("UserStatuses_Edit"), $"{Url}/{_active.Id}", new { name = "Changed", color = "x" })).Status.ShouldNotBe(HttpStatusCode.OK);

            Statuses().ShouldBe(["Active #00ff00", "Gone #ff0000"]);
        }


        // ================================================================
        // Deleting
        // ================================================================

        [Fact]
        public async Task A_row_is_deleted_by_its_id()
        {
            ApiResult<UserStatus> result = await DeleteAsync<UserStatus>(With("UserStatuses_Delete"), $"{Url}/{_active.Id}");

            result.ShouldSucceed(English.Deleted("UserStatus"));
            result.Value.Name.ShouldBe("Active");
            Statuses().ShouldBe(["Gone #ff0000"]);
        }

        [Fact]
        public async Task Deleting_needs_the_permission_to_delete()
        {
            (await DeleteAsync<UserStatus>(Without("Delete"), $"{Url}/{_active.Id}")).ShouldFail(English.NotAuthorized);

            Statuses().Count.ShouldBe(2);
        }

        [Fact]
        public async Task A_row_that_is_not_there_cannot_be_deleted()
        {
            (await DeleteAsync<UserStatus>(With("UserStatuses_Delete"), $"{Url}/99")).ShouldFail(English.NotFound("UserStatus"));
        }

        [Fact]
        public async Task An_id_that_is_not_a_number_breaks_a_delete()
        {
            (await DeleteAsync<UserStatus>(With("UserStatuses_Delete"), $"{Url}/abc")).ShouldBreak();
        }

        // Somebody has it, and the database will not let it go.
        [Fact]
        public async Task A_row_the_database_holds_on_to_breaks()
        {
            Seed.User(x => x.UserStatusId = _active.Id);

            (await DeleteAsync<UserStatus>(With("UserStatuses_Delete"), $"{Url}/{_active.Id}")).ShouldBreak();

            Statuses().Count.ShouldBe(2);
        }
    }

    /// <summary>
    /// The database saying it wrote nothing, which the real one never does.
    /// </summary>
    public sealed class StubbornRepositoryTests : IAsyncLifetime
    {
        private readonly TestDatabase _database = new TestDatabase();
        private ApiHost _host = null!;
        private HttpClient _client = null!;

        public async ValueTask InitializeAsync()
        {
            _host = await ApiHost.StartAsync(Settings.For(_database), services =>
            {
                ApiFixture.Quieten(services);
                services.AddScoped<IGenericRepository<UserStatus>, StubbornRepository>();
            });
            _client = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = _host.Address };
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _host.DisposeAsync();
            _database.Dispose();
        }

        [Fact]
        public async Task A_row_that_was_not_deleted_is_an_error()
        {
            Seed seed = new Seed(_database);
            UserStatus status = seed.UserStatus();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"api/UserStatuses/{status.Id}");
            Caller.Of(seed.User(), "UserStatuses_Delete").Sign(request);

            using HttpResponseMessage response = await _client.SendAsync(request, TestToken.Ct);

            ApiResult.Read<UserStatus>(response.StatusCode, await response.Content.ReadAsStringAsync(TestToken.Ct))
                .ShouldFail(English.Of(TranslationKeys.An_error_occurred_while_deleting_the_entity));
        }

        // Reads like the real one and deletes nothing.
        private sealed class StubbornRepository : GenericRepository<UserStatus>, IGenericRepository<UserStatus>
        {
            public StubbornRepository(IDbContextFactory<ApiDbContext> factory) : base(factory)
            {
            }

            Task<int> IGenericRepository<UserStatus>.RemoveAsync(UserStatus entity)
            {
                return Task.FromResult(0);
            }
        }
    }
}
