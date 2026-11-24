using API.Controllers;
using API.Tests.Host;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Roles and what they allow.
    /// </summary>
    public sealed class RolesControllerTests : ApiTest
    {
        private readonly User _user;
        private readonly Caller _anybody;

        // Each with the one permission it takes.
        private readonly Caller _adder;
        private readonly Caller _editor;
        private readonly Caller _deleter;

        public RolesControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _anybody = Caller.Of(_user);
            _adder = Caller.Of(_user, "Roles_Add");
            _editor = Caller.Of(_user, "Roles_Edit");
            _deleter = Caller.Of(_user, "Roles_Delete");
        }

        // Everything but the one it takes.
        private Caller AllBut(string permission)
        {
            string[] all = ["Roles_View", "Roles_Add", "Roles_Edit", "Roles_Delete", "Users_Add", "Users_Edit", "Users_Delete"];

            return Caller.Of(_user, all.Except([permission]).ToArray());
        }

        private static object Claims(string controller, bool view = false, bool add = false, bool edit = false, bool delete = false)
        {
            return new { controller, view, add, edit, delete };
        }

        private Role RoleWith(string name, params string[] claims)
        {
            Role role = Seed.Role(name);
            foreach (string claim in claims)
                Database.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = "Permission", ClaimValue = claim });

            return role;
        }

        private List<string> ClaimsOf(string role)
        {
            return Database.Read(db => db.RoleClaims
                .Where(x => db.Roles.Any(y => y.Id == x.RoleId && y.Name == role))
                .Select(x => x.ClaimType + " " + x.ClaimValue)
                .ToList());
        }


        // ================================================================
        // Reading
        // ================================================================

        // The id is compared as text, which an id cannot be, so a role cannot be read at all.
        [Fact]
        public async Task Reading_one_breaks()
        {
            Role role = Seed.Role("Trainer");

            (await GetAsync<RoleDto>(Caller.Of(_user, "Roles_View"), $"api/Roles/{role.Id}")).ShouldBreak();
        }

        [Fact]
        public async Task Reading_one_needs_the_permission()
        {
            (await GetAsync<RoleDto>(_anybody, $"api/Roles/{Guid.NewGuid()}")).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task The_grid_lists_the_roles()
        {
            Seed.Role("Trainer");
            Seed.Role("Administrator");

            DataTableDto<RoleDto> answer = (await PostAsync<DataTableDto<RoleDto>>(_anybody, "api/Roles/GetDataTable", SortedGrid<RoleDto>(("name", 1)))).Value;

            answer.Data!.Select(x => x.Name).ShouldBe(["Administrator", "Trainer"]);
            answer.Data!.ShouldAllBe(x => x.Claims.Count == 0);
            answer.TotalRecords.ShouldBe(2);
        }


        // ================================================================
        // Adding
        // ================================================================

        [Fact]
        public async Task Adding_takes_the_permission_and_no_other_will_do()
        {
            object[] form = [new { name = "Trainer", claims = new[] { Claims("Users", view: true) } }];

            (await PostAsync<List<Role>>(_anybody, "api/Roles", form)).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<Role>>(AllBut("Roles_Add"), "api/Roles", form)).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<Role>>(Caller.Of(_user, "roles_add"), "api/Roles", form)).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<Role>>(_anybody, "api/Roles", Array.Empty<object>())).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.Roles.Count()).ShouldBe(0);
        }

        // Every combination of the four boxes, each on a controller of its own.
        [Fact]
        public async Task A_new_role_may_do_what_is_ticked()
        {
            List<object> claims = new List<object>();
            List<string> expected = new List<string>();

            for (int boxes = 0; boxes < 16; boxes++)
            {
                (bool view, bool add, bool edit, bool delete) = ((boxes & 1) != 0, (boxes & 2) != 0, (boxes & 4) != 0, (boxes & 8) != 0);
                claims.Add(Claims("C" + boxes, view, add, edit, delete));

                if (view)
                    expected.Add($"Permission C{boxes}_View");
                if (add)
                    expected.Add($"Permission C{boxes}_Add");
                if (edit)
                    expected.Add($"Permission C{boxes}_Edit");
                if (delete)
                    expected.Add($"Permission C{boxes}_Delete");
            }

            ApiResult<List<Role>> result = await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name = "Trainer", claims } });

            result.ShouldSucceed(English.Updated("Role"));
            result.Value.Single().Name.ShouldBe("Trainer");
            ClaimsOf("Trainer").ShouldBe(expected, ignoreOrder: true);
            Database.Read(db => db.Roles.Single()).NormalizedName.ShouldBe("TRAINER");
        }

        [Fact]
        public async Task Several_roles_may_be_added_at_once()
        {
            (await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name = "One" }, new { name = "Two" } })).ShouldSucceed(English.Updated("Role"));

            Database.Read(db => db.Roles.Count()).ShouldBe(2);
        }

        [Fact]
        public async Task Adding_none_adds_none()
        {
            (await PostAsync<List<Role>>(_adder, "api/Roles", Array.Empty<object>())).ShouldSucceed(English.Updated("Role"));

            Database.Read(db => db.Roles.Count()).ShouldBe(0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task A_role_needs_a_name(string? name)
        {
            (await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name } })).ShouldFail(English.Required("Role name"));
        }

        [Fact]
        public async Task A_name_there_is_already_is_refused()
        {
            Seed.Role("Trainer");

            (await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name = "Trainer" } })).ShouldFail(English.Of(TranslationKeys._0_already_exists, "Role"));
        }

        // Found out one step later, where the reasons are not read out but the list they are in.
        [Fact]
        public async Task A_name_there_is_already_in_another_case_is_refused_with_no_reason()
        {
            Seed.Role("Trainer");

            (await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name = "TRAINER" } }))
                .ShouldFail("System.Collections.Generic.List`1[Microsoft.AspNetCore.Identity.IdentityError]");
        }

        // The ones before it are already there.
        [Fact]
        public async Task One_that_will_not_do_stops_the_ones_after_it()
        {
            Seed.Role("Trainer");

            (await PostAsync<List<Role>>(_adder, "api/Roles", new[] { new { name = "One" }, new { name = "Trainer" }, new { name = "Two" } }))
                .ShouldFail(English.Of(TranslationKeys._0_already_exists, "Role"));

            Database.Read(db => db.Roles.Select(x => x.Name).ToList()).ShouldBe(["Trainer", "One"], ignoreOrder: true);
        }


        // ================================================================
        // Editing
        // ================================================================

        [Fact]
        public async Task An_edited_role_may_do_what_is_ticked_and_nothing_else()
        {
            Role role = RoleWith("Trainer", "Mails_View", "Mails_Add", "Users_View");

            ApiResult<Role> result = await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new
            {
                id = role.Id,
                name = "Trainer",
                claims = new[] { Claims("Mails", view: true, edit: true), Claims("TrainGroups", delete: true, add: true), Claims("Users") }
            });

            result.ShouldSucceed(English.Updated("Role Trainer"));
            ClaimsOf("Trainer").ShouldBe(["Permission Mails_View", "Permission Mails_Edit", "Permission TrainGroups_Delete", "Permission TrainGroups_Add"], ignoreOrder: true);
        }

        [Fact]
        public async Task Nothing_ticked_leaves_nothing()
        {
            Role role = RoleWith("Trainer", "Mails_View", "Users_View");

            (await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new { id = role.Id, name = "Trainer" })).ShouldSucceed(English.Updated("Role Trainer"));

            ClaimsOf("Trainer").ShouldBeEmpty();
        }

        [Fact]
        public async Task What_is_ticked_already_stays_as_it_is()
        {
            Role role = RoleWith("Trainer", "Mails_View");
            int id = Database.Read(db => db.RoleClaims.Single()).Id;

            await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new { id = role.Id, name = "Trainer", claims = new[] { Claims("Mails", view: true) } });

            Database.Read(db => db.RoleClaims.Single()).Id.ShouldBe(id);
        }

        // The name in the form is only put in the message.
        [Fact]
        public async Task A_role_keeps_its_name()
        {
            Role role = RoleWith("Trainer");

            ApiResult<Role> result = await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new { id = role.Id, name = "Renamed" });

            result.ShouldSucceed(English.Updated("Role Renamed"));
            result.Value.Name.ShouldBe("Renamed");
            Database.Read(db => db.Roles.Single()).Name.ShouldBe("Trainer");
        }

        [Fact]
        public async Task Another_roles_claims_stay_as_they_are()
        {
            Role role = RoleWith("Trainer", "Mails_View");
            RoleWith("Administrator", "Mails_View", "Users_View");

            await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new { id = role.Id, name = "Trainer" });

            ClaimsOf("Administrator").ShouldBe(["Permission Mails_View", "Permission Users_View"], ignoreOrder: true);
        }

        [Fact]
        public async Task A_role_that_is_not_there_is_not_found()
        {
            (await PutAsync<Role>(_editor, $"api/Roles/{Guid.NewGuid()}", new { name = "Trainer" })).ShouldFail(English.Of(TranslationKeys._0_not_found, "Role name "));
        }

        [Fact]
        public async Task An_id_that_is_none_breaks()
        {
            (await PutAsync<Role>(_editor, "api/Roles/not-an-id", new { name = "Trainer" })).ShouldBreak();
        }

        // A role is what everybody else is checked against.
        [Fact]
        public async Task A_member_cannot_give_their_own_role_any_permission()
        {
            Role role = RoleWith("SimpleUser", "Subscriptions_View");
            Seed.UserRole(_user, role);

            (await PutAsync<Role>(Caller.Of(_user, "Subscriptions_View"), $"api/Roles/{role.Id}", new { id = role.Id, name = "SimpleUser", claims = new[] { Claims("Users", true, true, true, true) } }))
                .ShouldFail(English.NotAuthorized);

            ClaimsOf("SimpleUser").ShouldBe(["Permission Subscriptions_View"]);
        }

        [Fact]
        public async Task Editing_takes_the_permission_and_no_other_will_do()
        {
            Role role = RoleWith("Trainer", "Mails_View");
            object form = new { id = role.Id, name = "Trainer", claims = new[] { Claims("Users", view: true) } };

            (await PutAsync<Role>(_anybody, $"api/Roles/{role.Id}", form)).ShouldFail(English.NotAuthorized);
            (await PutAsync<Role>(AllBut("Roles_Edit"), $"api/Roles/{role.Id}", form)).ShouldFail(English.NotAuthorized);
            (await PutAsync<Role>(Caller.Of(_user, "roles_edit"), $"api/Roles/{role.Id}", form)).ShouldFail(English.NotAuthorized);

            ClaimsOf("Trainer").ShouldBe(["Permission Mails_View"]);
        }

        // Nobody finds out which roles there are by their ids.
        [Fact]
        public async Task Without_it_a_role_that_is_not_there_is_answered_the_same()
        {
            (await PutAsync<Role>(_anybody, $"api/Roles/{Guid.NewGuid()}", new { name = "Trainer" })).ShouldFail(English.NotAuthorized);
            (await PutAsync<Role>(_anybody, "api/Roles/not-an-id", new { name = "Trainer" })).ShouldFail(English.NotAuthorized);
        }


        // ================================================================
        // Deleting
        // ================================================================

        [Fact]
        public async Task Deleting_takes_the_permission_and_no_other_will_do()
        {
            Role role = Seed.Role("Trainer");

            (await DeleteAsync<Role>(_anybody, $"api/Roles/{role.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<Role>(AllBut("Roles_Delete"), $"api/Roles/{role.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<Role>(Caller.Of(_user, "roles_delete"), $"api/Roles/{role.Id}")).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.Roles.Count()).ShouldBe(1);
        }

        // Looked for by its id in text, which an id is not, so no role can be deleted.
        [Fact]
        public async Task Deleting_breaks()
        {
            Role role = Seed.Role("Trainer");

            (await DeleteAsync<Role>(_deleter, $"api/Roles/{role.Id}")).ShouldBreak();
            (await DeleteAsync<Role>(_deleter, "api/Roles/not-an-id")).ShouldBreak();

            Database.Read(db => db.Roles.Count()).ShouldBe(1);
        }


        // ================================================================
        // The dropdown
        // ================================================================

        private async Task<string> LookupAsync(string id, string value, int skip = 0, int take = 10)
        {
            ApiResult<LookupDto> result = await PostAsync<LookupDto>(_anybody, "api/Roles/Lookup", new { skip, take, filter = new { id, value } });

            result.ShouldSucceed();
            // Never counted.
            result.Value.TotalRecords.ShouldBe(0);

            return string.Join(" ", result.Value.Data.Select(x => x.Value).Order());
        }

        [Theory]
        [InlineData("", "Administrator SimpleUser Trainer")]
        [InlineData("tr", "Administrator Trainer")]
        [InlineData("TRAIN", "Trainer")]
        [InlineData("Tráiner", "Trainer")]
        [InlineData("x", "")]
        public async Task Roles_are_found_by_part_of_their_name(string value, string expected)
        {
            Seed.Role("Trainer");
            Seed.Role("Administrator");
            Seed.Role("SimpleUser");

            (await LookupAsync("", value)).ShouldBe(expected);
        }

        [Fact]
        public async Task One_role_is_found_by_its_id_whatever_is_typed()
        {
            Role trainer = Seed.Role("Trainer");
            Role administrator = Seed.Role("Administrator");

            (await LookupAsync(trainer.Id.ToString(), "Admin")).ShouldBe("Trainer");
            (await LookupAsync(trainer.Id.ToString().ToUpperInvariant(), "")).ShouldBe("Trainer");
            // Several ids in one text find several.
            (await LookupAsync($"{trainer.Id},{administrator.Id}", "")).ShouldBe("Administrator Trainer");
            (await LookupAsync(Guid.NewGuid().ToString(), "")).ShouldBe("");
            (await LookupAsync("not an id", "")).ShouldBe("");
        }

        [Fact]
        public async Task The_dropdown_comes_in_pages()
        {
            for (int i = 0; i < 5; i++)
                Seed.Role("Role " + i);

            (await LookupAsync("", "", 0, 2)).Split(' ').Length.ShouldBe(4);
            (await LookupAsync("", "", 4, 2)).Split(' ').Length.ShouldBe(2);
            (await LookupAsync("", "", 5, 2)).ShouldBe("");
            (await LookupAsync("", "", 0, 0)).ShouldBe("");
        }

        [Fact]
        public async Task Each_comes_with_its_id()
        {
            Role role = Seed.Role("Trainer");

            (await PostAsync<LookupDto>(_anybody, "api/Roles/Lookup", new { take = 10, filter = new { } })).Value.Data.Single().Id.ShouldBe(role.Id.ToString());
        }

        [Fact]
        public async Task A_dropdown_needs_a_filter()
        {
            (await PostAsync<LookupDto>(_anybody, "api/Roles/Lookup", new { take = 10 })).ShouldBeBad(English.Required("Filter"));
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // The controller by itself, with a say in what it is given.
        private RolesController Alone(params object[] given)
        {
            return Direct.Controller<RolesController>(Services, "Roles", ["Roles_Edit", "Roles_Delete"], _user, given);
        }

        private DataService DataServiceWith(IGenericRepository<Role> roles)
        {
            return ActivatorUtilities.CreateInstance<DataService>(Services.CreateScope().ServiceProvider, roles);
        }

        // The address always has an id in it.
        [Fact]
        public async Task Without_an_id_there_is_nothing_to_edit_or_delete()
        {
            ActionResult<ApiResponse<Role>> edited = await Alone().Put(null, new RoleDto());
            ActionResult<ApiResponse<Role>> deleted = await Alone().Delete(null);

            edited.Value.ShouldNotBeNull().Messages["error"].ShouldBe([English.Required("RoleId")]);
            deleted.Value.ShouldNotBeNull().Messages["error"].ShouldBe([English.Required("RoleId")]);
        }

        // What deleting would do if the role were found.
        [Fact]
        public async Task A_role_that_was_found_would_be_deleted()
        {
            Role role = Seed.Role("Trainer");

            ActionResult<ApiResponse<Role>> deleted = await Alone(DataServiceWith(new RolesByText(Database.Factory))).Delete(role.Id.ToString());

            deleted.Value.ShouldNotBeNull().IsSucceed.ShouldBeTrue();
            deleted.Value.Messages["success"].ShouldBe([English.Deleted("Role Trainer")]);
            Database.Read(db => db.Roles.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_role_that_was_not_found_would_be_said_so()
        {
            ActionResult<ApiResponse<Role>> deleted = await Alone(DataServiceWith(new RolesByText(Database.Factory))).Delete(Guid.NewGuid().ToString());

            deleted.Value.ShouldNotBeNull().Messages["error"].ShouldBe([English.Of(TranslationKeys._0_not_found, "Role")]);
        }

        // Somebody else changed it in between. The reasons are not read out but the list they are in.
        [Fact]
        public async Task A_role_that_could_not_be_deleted_would_be_said_so_with_no_reason()
        {
            Role role = Seed.Role("Trainer");
            RolesByText changed = new RolesByText(Database.Factory) { IsChangedMeanwhile = true };

            ActionResult<ApiResponse<Role>> deleted = await Alone(DataServiceWith(changed)).Delete(role.Id.ToString());

            deleted.Value.ShouldNotBeNull().Messages["error"].ShouldBe(["System.Collections.Generic.List`1[Microsoft.AspNetCore.Identity.IdentityError]"]);
            Database.Read(db => db.Roles.Count()).ShouldBe(1);
        }

        // Finds a role by its id in text.
        private sealed class RolesByText : GenericRepository<Role>, IGenericRepository<Role>
        {
            public RolesByText(IDbContextFactory<ApiDbContext> factory) : base(factory)
            {
            }

            public bool IsChangedMeanwhile { get; set; }

            async Task<Role?> IGenericRepository<Role>.FindAsync(object? id)
            {
                Role? role = await FindAsync(Guid.Parse((string)id!));

                if (role != null && IsChangedMeanwhile)
                    role.ConcurrencyStamp = "older";

                return role;
            }
        }

        // A dropdown that names no filter, or one with nothing in it, is every role.
        [Fact]
        public async Task A_dropdown_with_no_filter_finds_every_role()
        {
            Seed.Role("Trainer");

            (await Alone().Lookup(new LookupDto { Take = 10 })).Data.ShouldNotBeNull().Data.Single().Value.ShouldBe("Trainer");
            (await Alone().Lookup(new LookupDto { Take = 10, Filter = new LookupFilterDto { Id = null!, Value = null! } })).Data.ShouldNotBeNull().Data.Single().Value.ShouldBe("Trainer");
        }
    }
}
