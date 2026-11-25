using System.Security.Claims;
using API.Controllers;
using API.Tests.Host;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // A dozen of them, each saved before the answer is given.
        [Fact]
        public async Task Everything_is_saved_by_the_time_it_is_answered()
        {
            Role role = RoleWith("Trainer", "Mails_View", "Mails_Add", "Mails_Edit", "Mails_Delete", "Users_View", "Users_Add");
            SlowRoles slow = ActivatorUtilities.CreateInstance<SlowRoles>(Services.CreateScope().ServiceProvider);

            ActionResult<ApiResponse<Role>> result = await Alone(slow).Put(role.Id.ToString(), new RoleDto
            {
                Name = "Trainer",
                Claims = [new IdentityClaimDto { Controller = "Roles", View = true, Add = true, Edit = true, Delete = true }, new IdentityClaimDto { Controller = "Users", View = true, Delete = true }]
            });

            result.Value.ShouldNotBeNull().Messages["success"].ShouldBe([English.Updated("Role Trainer")]);
            ClaimsOf("Trainer").ShouldBe(
                ["Permission Users_View", "Permission Roles_View", "Permission Roles_Add", "Permission Roles_Edit", "Permission Roles_Delete", "Permission Users_Delete"], ignoreOrder: true);
        }

        // Somebody else changed the role in between, say. It is not called updated, and the rest is left.
        [Theory]
        [InlineData("Mails_View", "Mails_View Mails_Add")]
        [InlineData("Mails_Add", "Mails_Add")]
        [InlineData("Users_View", "")]
        [InlineData("Users_Add", "Users_View")]
        public async Task A_permission_that_cannot_be_saved_is_said_so(string refused, string left)
        {
            Role role = RoleWith("Trainer", "Mails_View", "Mails_Add");
            RefusingRoles refusing = ActivatorUtilities.CreateInstance<RefusingRoles>(Services.CreateScope().ServiceProvider);
            refusing.Refused = refused;

            ActionResult<ApiResponse<Role>> result = await Alone(refusing).Put(role.Id.ToString(), new RoleDto { Name = "Trainer", Claims = [new IdentityClaimDto { Controller = "Users", View = true, Add = true }] });

            result.Value.ShouldNotBeNull().IsSucceed.ShouldBeFalse();
            result.Value.Messages.ShouldHaveSingleItem().Value.ShouldBe(["Changed meanwhile."]);
            ClaimsOf("Trainer").ShouldBe(left.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => "Permission " + x), ignoreOrder: true);
        }

        // The database will not be written to. With nobody waiting for it there was nobody to catch it
        // either, and the whole server was down.
        [Theory]
        [InlineData("Mails_View")]
        [InlineData("Users_View")]
        public async Task A_database_that_cannot_be_written_to_breaks_this_request_and_no_other(string broken)
        {
            Role role = RoleWith("Trainer", "Mails_View");
            SlowRoles breaking = ActivatorUtilities.CreateInstance<SlowRoles>(Services.CreateScope().ServiceProvider);
            breaking.Broken = broken;

            await Should.ThrowAsync<InvalidOperationException>(() => Alone(breaking).Put(role.Id.ToString(), new RoleDto { Name = "Trainer", Claims = [new IdentityClaimDto { Controller = "Users", View = true }] }));

            (await PutAsync<Role>(_editor, $"api/Roles/{role.Id}", new { id = role.Id, name = "Trainer", claims = new[] { Claims("Users", view: true) } })).ShouldSucceed(English.Updated("Role Trainer"));
            ClaimsOf("Trainer").ShouldBe(["Permission Users_View"]);
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

        [Fact]
        public async Task A_role_nobody_has_is_deleted_with_what_it_allows()
        {
            Role role = RoleWith("Trainer", "Mails_View", "Mails_Add");
            Seed.UserRole(_user, RoleWith("Administrator", "Mails_View"));

            (await DeleteAsync<Role>(_deleter, $"api/Roles/{role.Id}")).ShouldSucceed(English.Deleted("Role Trainer"));

            Database.Read(db => db.Roles.Single()).Name.ShouldBe("Administrator");
            Database.Read(db => db.RoleClaims.Single()).ClaimValue.ShouldBe("Mails_View");
            Database.Read(db => db.UserRoles.Count()).ShouldBe(1);
        }

        // They would be left with no role, and could do nothing at all.
        [Fact]
        public async Task A_role_somebody_has_is_not()
        {
            Role role = RoleWith("Trainer", "Mails_View");
            UserRole theirs = Seed.UserRole(_user, role);

            (await DeleteAsync<Role>(_deleter, $"api/Roles/{role.Id}")).ShouldFail(English.Of(TranslationKeys.The_role_is_in_use_by_members));

            ClaimsOf("Trainer").ShouldBe(["Permission Mails_View"]);
            Database.Read(db => db.UserRoles.Single()).RoleId.ShouldBe(role.Id);

            // Until nobody has it any more.
            Database.Change(db => db.UserRoles.Remove(theirs));

            (await DeleteAsync<Role>(_deleter, $"api/Roles/{role.Id}")).ShouldSucceed(English.Deleted("Role Trainer"));
        }

        [Theory]
        [InlineData("not-an-id")]
        [InlineData("7")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-2222-3333-4444-555555555555")]
        public async Task A_role_that_is_not_there_cannot_be_deleted(string id)
        {
            Seed.Role("Trainer");

            (await DeleteAsync<Role>(_deleter, $"api/Roles/{id}")).ShouldFail(English.Of(TranslationKeys._0_not_found, "Role"));

            Database.Read(db => db.Roles.Count()).ShouldBe(1);
        }

        // Nobody finds out who has which role by deleting it.
        [Fact]
        public async Task Without_the_permission_every_role_is_answered_the_same()
        {
            Role role = Seed.Role("Trainer");
            Seed.UserRole(_user, role);

            (await DeleteAsync<Role>(_anybody, $"api/Roles/{role.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<Role>(_anybody, $"api/Roles/{Guid.NewGuid()}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<Role>(_anybody, "api/Roles/not-an-id")).ShouldFail(English.NotAuthorized);
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

        // The address always has an id in it.
        [Fact]
        public async Task Without_an_id_there_is_nothing_to_edit_or_delete()
        {
            ActionResult<ApiResponse<Role>> edited = await Alone().Put(null, new RoleDto());
            ActionResult<ApiResponse<Role>> deleted = await Alone().Delete(null);

            edited.Value.ShouldNotBeNull().Messages["error"].ShouldBe([English.Required("RoleId")]);
            deleted.Value.ShouldNotBeNull().Messages["error"].ShouldBe([English.Required("RoleId")]);
        }

        // Somebody else changed it in between.
        [Fact]
        public async Task A_role_that_could_not_be_deleted_is_said_so()
        {
            Role role = Seed.Role("Trainer");
            RefusingRoles refusing = ActivatorUtilities.CreateInstance<RefusingRoles>(Services.CreateScope().ServiceProvider);

            ActionResult<ApiResponse<Role>> deleted = await Alone(refusing).Delete(role.Id.ToString());

            deleted.Value.ShouldNotBeNull().Messages["error"].ShouldBe(["Changed meanwhile."]);
            Database.Read(db => db.Roles.Count()).ShouldBe(1);
        }

        // Takes its time over each permission, as a database does that is not on the same machine, and
        // may break on one.
        private sealed class SlowRoles : RoleManager<Role>
        {
            public SlowRoles(IRoleStore<Role> store, IEnumerable<IRoleValidator<Role>> validators, ILookupNormalizer normalizer, IdentityErrorDescriber errors, ILogger<RoleManager<Role>> logger)
                : base(store, validators, normalizer, errors, logger)
            {
            }

            public string? Broken { get; set; }

            public override async Task<IdentityResult> AddClaimAsync(Role role, Claim claim)
            {
                await WaitAsync(claim);

                return await base.AddClaimAsync(role, claim);
            }

            public override async Task<IdentityResult> RemoveClaimAsync(Role role, Claim claim)
            {
                await WaitAsync(claim);

                return await base.RemoveClaimAsync(role, claim);
            }

            private async Task WaitAsync(Claim claim)
            {
                await Task.Delay(20, TestContext.Current.CancellationToken);

                if (claim.Value == Broken)
                    throw new InvalidOperationException("database is locked");
            }
        }

        // Saves everything but the one, and deletes nothing.
        private sealed class RefusingRoles : RoleManager<Role>
        {
            private static readonly IdentityResult No = IdentityResult.Failed(new IdentityError { Description = "Changed meanwhile." }, new IdentityError { Description = "And more." });

            public RefusingRoles(IRoleStore<Role> store, IEnumerable<IRoleValidator<Role>> validators, ILookupNormalizer normalizer, IdentityErrorDescriber errors, ILogger<RoleManager<Role>> logger)
                : base(store, validators, normalizer, errors, logger)
            {
            }

            public string? Refused { get; set; }

            public override Task<IdentityResult> AddClaimAsync(Role role, Claim claim)
            {
                return claim.Value == Refused ? Task.FromResult(No) : base.AddClaimAsync(role, claim);
            }

            public override Task<IdentityResult> RemoveClaimAsync(Role role, Claim claim)
            {
                return claim.Value == Refused ? Task.FromResult(No) : base.RemoveClaimAsync(role, claim);
            }

            public override Task<IdentityResult> DeleteAsync(Role role)
            {
                return Task.FromResult(No);
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
