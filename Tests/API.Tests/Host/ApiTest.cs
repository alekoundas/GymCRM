using API.Tests.Fakes;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace API.Tests.Host
{
    /// <summary>
    /// A test of the app from the outside: requests in, answers out, and the rows underneath. The
    /// database is empty before each.
    /// </summary>
    public abstract class ApiTest : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _api;

        protected ApiTest(ApiFixture api)
        {
            _api = api;
            _api.Reset();

            Seed = new Seed(api.Database);
        }

        protected Seed Seed { get; }

        protected TestDatabase Database
        {
            get { return _api.Database; }
        }

        protected RecordingEmailService Emails
        {
            get { return _api.Emails; }
        }

        protected StubGoogle Google
        {
            get { return _api.Google; }
        }

        protected IServiceProvider Services
        {
            get { return _api.Services; }
        }

        protected Uri Address
        {
            get { return _api.Address; }
        }

        protected Task<ApiResult<T>> GetAsync<T>(Caller caller, string url, string? language = null)
        {
            return SendAsync<T>(HttpMethod.Get, caller, url, null, language);
        }

        protected Task<ApiResult<T>> PostAsync<T>(Caller caller, string url, object? body, string? language = null)
        {
            return SendAsync<T>(HttpMethod.Post, caller, url, body, language);
        }

        protected Task<ApiResult<T>> PutAsync<T>(Caller caller, string url, object? body, string? language = null)
        {
            return SendAsync<T>(HttpMethod.Put, caller, url, body, language);
        }

        protected Task<ApiResult<T>> DeleteAsync<T>(Caller caller, string url, string? language = null)
        {
            return SendAsync<T>(HttpMethod.Delete, caller, url, null, language);
        }

        protected async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, Caller caller, string url, object? body, string? language = null)
        {
            using HttpResponseMessage response = await _api.SendAsync(method, url, body, caller, language);
            string answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            // Whatever a test is about: nobody is ever sent what an account signs in with.
            answer.ShouldNotContain("\"passwordHash\":");
            answer.ShouldNotContain("\"securityStamp\":");

            return ApiResult.Read<T>(response.StatusCode, answer);
        }

        protected Task<HttpResponseMessage> RawAsync(HttpMethod method, Caller caller, string url, object? body = null, string? language = null)
        {
            return _api.SendAsync(method, url, body, caller, language);
        }


        // A grid's request.

        protected static DataTableDto<T> Grid<T>(params DataTableFilterDto[] filters)
        {
            return new DataTableDto<T> { Rows = 50, Page = 0, Filters = filters };
        }

        protected static DataTableDto<T> SortedGrid<T>(params (string Field, int Order)[] sorts)
        {
            return new DataTableDto<T> { Rows = 50, Page = 0, Sorts = sorts.Select(x => new DataTableSortDto { FieldName = x.Field, Order = x.Order }).ToList() };
        }

        protected static DataTableFilterDto Filter(string field, string? value, DataTableFiltersEnum type = DataTableFiltersEnum.contains)
        {
            return new DataTableFilterDto { FieldName = field, Value = value, FilterType = type };
        }

        protected static DataTableFilterDto Filter(string field, IEnumerable<string> values, DataTableFiltersEnum type)
        {
            return new DataTableFilterDto { FieldName = field, Values = values.ToList(), FilterType = type };
        }


        // A member's clock. Every endpoint that asks what time it is where the member is takes it
        // from the minutes the browser is off UTC, so any time at all can be had that way.

        /// <summary>
        /// The offset that makes it <c>now</c> for the member, or up to a minute after. A test of a moment
        /// stays two minutes off it on the near side.
        /// </summary>
        protected static int OffsetFor(DateTime now)
        {
            return (int)Math.Floor((DateTime.UtcNow - now).TotalMinutes);
        }

        protected static Caller Staff(User user)
        {
            return Caller.Of(user, "TrainGroupParticipants_View");
        }
    }
}
