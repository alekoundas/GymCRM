using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The boxes of the role form: one row for everything any role may do anything with, ticked as
    /// far as the role may.
    /// </summary>
    public sealed class ClaimsControllerTests : ApiTest
    {
        private readonly Caller _anybody;

        public ClaimsControllerTests(ApiFixture api) : base(api)
        {
            _anybody = Caller.Of(Seed.User());

            Add("Trainer", "Mails_View", "Mails_Add", "TrainGroups_Edit");
            Add("Administrator", "Mails_View", "Users_View", "Users_Add", "Users_Edit", "Users_Delete");
            Add("Empty");
        }

        private void Add(string name, params string[] claims)
        {
            Role role = Seed.Role(name);
            foreach (string claim in claims)
                Database.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = "Permission", ClaimValue = claim });
        }

        private async Task<List<string>> RowsAsync(params DataTableFilterDto[] filters)
        {
            ApiResult<DataTableDto<IdentityClaimDto>> result = await PostAsync<DataTableDto<IdentityClaimDto>>(_anybody, "api/Claims/GetDataTable", Grid<IdentityClaimDto>(filters));

            result.ShouldSucceed();
            result.Value.TotalRecords.ShouldBe(result.Value.Data!.Count());
            result.Value.PageCount.ShouldBe(1);

            return result.Value.Data!.Select(x => $"{x.Controller} {(x.View ? "V" : "-")}{(x.Add ? "A" : "-")}{(x.Edit ? "E" : "-")}{(x.Delete ? "D" : "-")}").ToList();
        }

        [Fact]
        public async Task A_new_role_starts_with_nothing_ticked()
        {
            (await RowsAsync()).ShouldBe(["Mails ----", "TrainGroups ----", "Users ----"], ignoreOrder: true);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_role_with_no_name_is_a_new_one(string? name)
        {
            (await RowsAsync(Filter("RoleName", name))).ShouldBe(["Mails ----", "TrainGroups ----", "Users ----"], ignoreOrder: true);
        }

        [Theory]
        [InlineData("Trainer", "Mails VA--", "TrainGroups --E-", "Users ----")]
        [InlineData("TRAINER", "Mails VA--", "TrainGroups --E-", "Users ----")]
        [InlineData("Administrator", "Mails V---", "TrainGroups ----", "Users VAED")]
        [InlineData("Empty", "Mails ----", "TrainGroups ----", "Users ----")]
        public async Task A_role_that_is_there_is_ticked_as_far_as_it_may(string name, string mails, string trainGroups, string users)
        {
            (await RowsAsync(Filter("RoleName", name))).ShouldBe([mails, trainGroups, users], ignoreOrder: true);
        }

        [Fact]
        public async Task A_role_that_is_not_there_has_no_rows()
        {
            (await RowsAsync(Filter("RoleName", "Nobody"))).ShouldBeEmpty();
            (await RowsAsync(Filter("RoleName", "N"))).ShouldBeEmpty();
        }

        // Only under exactly that name.
        [Fact]
        public async Task Any_other_filter_is_passed_over()
        {
            (await RowsAsync(Filter("roleName", "Trainer"), Filter("Name", "Trainer"))).ShouldBe(["Mails ----", "TrainGroups ----", "Users ----"], ignoreOrder: true);
        }

        [Fact]
        public async Task A_gym_with_no_roles_has_no_rows()
        {
            Database.Reset();

            (await RowsAsync()).ShouldBeEmpty();
        }

        // Cut at the first underscore, whatever comes after it.
        [Fact]
        public async Task A_permission_with_no_action_is_a_row_like_any_other()
        {
            Add("Odd", "Mails", "Odd_One_View");

            (await RowsAsync(Filter("RoleName", "Odd"))).ShouldBe(["Mails ----", "TrainGroups ----", "Users ----", "Odd ----"], ignoreOrder: true);
        }
    }
}
