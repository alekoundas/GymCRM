using System.Linq.Expressions;
using API.Controllers;
using API.Tests.Host;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.Identity;
using Core.Dtos.User;
using Core.Dtos.UserRole;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Tests.Controllers.Users
{
    /// <summary>
    /// A member's record: read, changed by themselves or by an admin, and deleted.
    /// </summary>
    public sealed class ProfileTests : ApiTest
    {
        private const string Url = "api/Users";
        private const int MaxImageBytes = 1024 * 1024;

        private static readonly byte[] PngStart = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;
        private readonly Role _trainer;
        private readonly Role _member;
        private readonly UserStatus _active;

        public ProfileTests(ApiFixture api) : base(api)
        {
            _active = Seed.UserStatus(x => { x.Name = "Active"; x.Color = "#00ff00"; });
            _maria = Seed.User(x =>
            {
                x.UserName = "maria";
                x.Email = "maria@gym.test";
                x.FirstName = "Maria";
                x.LastName = "Papa";
                x.Address = "Street 1";
                x.MedicalHistory = "Knee";
                x.ProfileImage = Image(20);
                x.UserStatusId = _active.Id;
                x.PasswordHash = "hash";
                x.PhoneNumber = "2101234567";
            });
            _nikos = Seed.User(x => { x.UserName = "nikos"; x.Email = "nikos@gym.test"; });
            _admin = Seed.User();
            _trainer = Seed.Role("Trainer");
            _member = Seed.Role("SimpleUser");
            Seed.UserRole(_maria, _member);
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, "Users_View", "Users_Add", "Users_Edit", "Users_Delete"); }
        }

        private static byte[] Image(int bytes)
        {
            byte[] image = new byte[bytes];
            PngStart.CopyTo(image, 0);

            return image;
        }

        private User Read(Guid? id = null)
        {
            return Database.Read(db => db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).Single(x => x.Id == (id ?? _maria.Id)));
        }

        private string RolesOf(Guid? id = null)
        {
            return string.Join(" ", Read(id).UserRoles.Select(x => x.Role.Name));
        }


        // ================================================================
        // Reading one
        // ================================================================

        [Fact]
        public async Task A_member_reads_their_own_record()
        {
            ApiResult<UserDto> result = await GetAsync<UserDto>(Caller.Of(_maria), $"{Url}/{_maria.Id}");

            result.ShouldSucceed();
            UserDto dto = result.Value;
            dto.Id.ShouldBe(_maria.Id.ToString());
            dto.UserName.ShouldBe("maria");
            dto.Email.ShouldBe("maria@gym.test");
            dto.FirstName.ShouldBe("Maria");
            dto.LastName.ShouldBe("Papa");
            dto.Address.ShouldBe("Street 1");
            dto.MedicalHistory.ShouldBe("Knee");
            dto.ProfileImage.ShouldBe(Image(20));
            dto.UserStatusId.ShouldBe(_active.Id);
            dto.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
            dto.UserRoles.Single().Role.Name.ShouldBe("SimpleUser");
            dto.UserRoles.Single().Role.Id.ShouldBe(_member.Id.ToString());
            // Only the grid has it.
            dto.SubscriptionBalance.ShouldBe(0);
        }

        // Nothing one could sign in with.
        [Fact]
        public async Task The_password_and_its_stamps_stay_where_they_are()
        {
            string body = (await GetAsync<UserDto>(Admin, $"{Url}/{_maria.Id}")).Body;

            body.ShouldNotContain("hash", Case.Insensitive);
            body.ShouldNotContain(_maria.SecurityStamp!);
            body.ShouldNotContain(_maria.ConcurrencyStamp!);
        }

        // Somebody else's answers like one that is not there.
        [Fact]
        public async Task Somebody_elses_needs_the_permission_to_view_members()
        {
            (await GetAsync<UserDto>(Caller.Of(_nikos, "Users_View"), $"{Url}/{_maria.Id}")).Value.UserName.ShouldBe("maria");

            (await GetAsync<UserDto>(Caller.Of(_nikos, "Users_Edit", "Users_Add", "Users_Delete", "TrainGroupParticipants_View"), $"{Url}/{_maria.Id}")).ShouldFail(English.Of(TranslationKeys._0_not_found, "User"));
            (await GetAsync<UserDto>(Caller.WithNoId(), $"{Url}/{_maria.Id}")).ShouldFail(English.Of(TranslationKeys._0_not_found, "User"));
        }

        [Fact]
        public async Task Somebody_who_is_not_there_is_not_found()
        {
            string notFound = English.Of(TranslationKeys._0_not_found, "User");

            (await GetAsync<UserDto>(Admin, $"{Url}/{Guid.NewGuid()}")).ShouldFail(notFound);
            (await GetAsync<UserDto>(Admin, $"{Url}/x")).ShouldFail(notFound);

            // Their own, after the account is gone.
            User gone = new User { Id = Guid.NewGuid() };
            (await GetAsync<UserDto>(Caller.Of(gone), $"{Url}/{gone.Id}")).ShouldFail(notFound);
        }

        [Fact]
        public async Task An_id_may_be_written_in_any_way_one_is()
        {
            (await GetAsync<UserDto>(Caller.Of(_maria), $"{Url}/{_maria.Id.ToString().ToUpperInvariant()}")).ShouldSucceed();
            (await GetAsync<UserDto>(Caller.Of(_maria), $"{Url}/{_maria.Id:N}")).ShouldSucceed();
        }


        // ================================================================
        // One's own profile
        // ================================================================

        private UserDto Form(Action<UserDto>? change = null)
        {
            UserDto form = new UserDto
            {
                Id = _maria.Id.ToString(),
                UserName = "maria",
                Email = "maria@gym.test",
                FirstName = "Maria",
                LastName = "Papa",
                Address = "Street 1",
                MedicalHistory = "Knee",
                ProfileImage = Image(20),
                UserStatusId = _active.Id,
                UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "SimpleUser" } }]
            };
            change?.Invoke(form);

            return form;
        }

        private Task<ApiResult<UserDto>> SaveAsync(Caller caller, UserDto form, string? id = null)
        {
            return PutAsync<UserDto>(caller, $"{Url}/{id ?? form.Id}", form);
        }

        [Fact]
        public async Task A_member_changes_what_is_theirs_and_nothing_else()
        {
            Role admin = Seed.Role("Administrator");
            UserStatus other = Seed.UserStatus();

            ApiResult<UserDto> result = await SaveAsync(Caller.Of(_maria), Form(x =>
            {
                x.FirstName = "Μαρία";
                x.LastName = "Παπά";
                x.Address = "Street 2";
                x.MedicalHistory = "Knee and back";
                x.ProfileImage = Image(30);
                x.UserName = "changed";
                x.Email = "changed@gym.test";
                x.UserStatusId = other.Id;
                x.SubscriptionBalance = 99;
                x.UserRoles = [new UserRoleDto { Role = new RoleDto { Id = admin.Id.ToString(), Name = "Administrator" } }];
            }));

            result.ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            User changed = Read();
            changed.FirstName.ShouldBe("Μαρία");
            changed.LastName.ShouldBe("Παπά");
            changed.Address.ShouldBe("Street 2");
            changed.MedicalHistory.ShouldBe("Knee and back");
            changed.ProfileImage.ShouldBe(Image(30));
            changed.UserName.ShouldBe("maria");
            changed.Email.ShouldBe("maria@gym.test");
            changed.UserStatusId.ShouldBe(_active.Id);
            changed.PasswordHash.ShouldBe("hash");
            changed.PhoneNumber.ShouldBe("2101234567");
            RolesOf().ShouldBe("SimpleUser");

            // What comes back is what is there.
            result.Value.FirstName.ShouldBe("Μαρία");
            result.Value.UserName.ShouldBe("maria");
            result.Value.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
            result.Value.UserRoles.Single().Role.Name.ShouldBe("SimpleUser");
        }

        // The profile page has no role to send.
        [Fact]
        public async Task A_member_need_not_send_a_role()
        {
            (await SaveAsync(Caller.Of(_maria), Form(x => x.UserRoles = []))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
        }

        [Fact]
        public async Task A_member_cannot_change_somebody_else()
        {
            (await SaveAsync(Caller.Of(_nikos, "Users_View", "Users_Add", "Users_Delete"), Form(x => x.FirstName = "Changed"))).ShouldFail(English.NotAuthorized);
            (await SaveAsync(Caller.WithNoId("Users_View"), Form(x => x.FirstName = "Changed"))).ShouldFail(English.NotAuthorized);

            Read().FirstName.ShouldBe("Maria");
        }

        // An account with a name that is no longer allowed cannot be saved at all.
        [Fact]
        public async Task What_the_accounts_refuse_is_said_in_their_words()
        {
            Database.Execute("UPDATE AspNetUsers SET UserName = 'maria papa' WHERE UserName = 'maria'");

            (await SaveAsync(Caller.Of(_maria), Form(x => x.FirstName = "Changed"))).ShouldFail("Username 'maria papa' is invalid, can only contain letters or digits.");

            Read().FirstName.ShouldBe("Maria");
        }


        // ================================================================
        // The picture
        // ================================================================

        // A megabyte, and what it really is: for a member and for an admin.
        [Fact]
        public async Task A_picture_is_one_and_not_too_big()
        {
            string error = English.Of(TranslationKeys.Image_must_be_a_JPG_PNG_or_WebP_of_up_to_0_MB, "1");

            foreach (Caller caller in new[] { Caller.Of(_maria), Admin })
            {
                foreach (byte[] refused in new byte[][] { [1, 2, 3], Image(MaxImageBytes + 1) })
                    (await SaveAsync(caller, Form(x => { x.FirstName = "Changed"; x.ProfileImage = refused; }))).ShouldFail(error);

                Read().ProfileImage.ShouldNotBe([1, 2, 3]);
                Read().FirstName.ShouldBe("Maria");

                (await SaveAsync(caller, Form(x => x.ProfileImage = Image(MaxImageBytes)))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
                Read().ProfileImage!.Length.ShouldBe(MaxImageBytes);
            }
        }

        // One from before pictures were looked at does not stop the rest being saved.
        [Fact]
        public async Task A_picture_that_stays_is_not_looked_at_again()
        {
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).ProfileImage = [1, 2, 3]);

            (await SaveAsync(Caller.Of(_maria), Form(x => { x.FirstName = "Changed"; x.ProfileImage = [1, 2, 3]; }))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            Read().FirstName.ShouldBe("Changed");
        }

        [Fact]
        public async Task A_picture_can_be_taken_away_and_put_on()
        {
            await SaveAsync(Caller.Of(_maria), Form(x => x.ProfileImage = null));
            Read().ProfileImage.ShouldBeNull();

            // None and none again is no change.
            (await SaveAsync(Caller.Of(_maria), Form(x => x.ProfileImage = []))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            Read().ProfileImage.ShouldBe([]);

            await SaveAsync(Caller.Of(_maria), Form(x => x.ProfileImage = Image(25)));
            Read().ProfileImage.ShouldBe(Image(25));
        }


        // ================================================================
        // The admin's form
        // ================================================================

        [Fact]
        public async Task An_admin_changes_everything_the_form_has()
        {
            UserStatus other = Seed.UserStatus(x => x.Name = "Away");

            ApiResult<UserDto> result = await SaveAsync(Admin, Form(x =>
            {
                x.UserName = "changed";
                x.Email = "changed@gym.test";
                x.FirstName = "Μαρία";
                x.LastName = "Παπά";
                x.Address = "Street 2";
                x.MedicalHistory = "Back";
                x.ProfileImage = Image(30);
                x.UserStatusId = other.Id;
                x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "Trainer" } }];
            }));

            result.ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            User changed = Read();
            changed.UserName.ShouldBe("changed");
            changed.NormalizedUserName.ShouldBe("CHANGED");
            changed.Email.ShouldBe("changed@gym.test");
            changed.NormalizedEmail.ShouldBe("CHANGED@GYM.TEST");
            changed.FirstName.ShouldBe("Μαρία");
            changed.LastName.ShouldBe("Παπά");
            changed.Address.ShouldBe("Street 2");
            changed.MedicalHistory.ShouldBe("Back");
            changed.ProfileImage.ShouldBe(Image(30));
            changed.UserStatusId.ShouldBe(other.Id);
            changed.PasswordHash.ShouldBe("hash");
            changed.SecurityStamp.ShouldBe(_maria.SecurityStamp);
            changed.PhoneNumber.ShouldBe("2101234567");
            RolesOf().ShouldBe("Trainer");

            result.Value.UserName.ShouldBe("changed");
            result.Value.Email.ShouldBe("changed@gym.test");
        }

        [Fact]
        public async Task The_permission_alone_changes_anybody_and_oneself()
        {
            (await SaveAsync(Caller.Of(_nikos, "Users_Edit"), Form(x => x.FirstName = "One"))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            (await SaveAsync(Caller.WithNoId("Users_Edit"), Form(x => x.FirstName = "Two"))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));
            (await SaveAsync(Caller.Of(_maria, "Users_Edit"), Form(x => x.UserName = "three"))).ShouldSucceed(English.Of(TranslationKeys._0_updated_successfully, "User"));

            Read().UserName.ShouldBe("three");
        }

        // A status left out is taken away.
        [Fact]
        public async Task A_status_can_be_taken_away()
        {
            await SaveAsync(Admin, Form(x => x.UserStatusId = null));

            Read().UserStatusId.ShouldBeNull();
            (await SaveAsync(Admin, Form(x => x.UserStatusId = 99))).ShouldBreak();
        }

        [Fact]
        public async Task An_admin_has_to_say_which_role()
        {
            (await SaveAsync(Admin, Form(x => x.UserRoles = []))).ShouldFail(English.Required("Role"));
            (await SaveAsync(Admin, Form(x => x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "Nobody" } }]))).ShouldFail(English.Of(TranslationKeys._0_not_found, "Role"));
            (await SaveAsync(Admin, Form(x => x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = null } }]))).ShouldFail(English.Of(TranslationKeys._0_not_found, "Role"));
            // Letter for letter.
            (await SaveAsync(Admin, Form(x => x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "trainer" } }]))).ShouldFail(English.Of(TranslationKeys._0_not_found, "Role"));

            RolesOf().ShouldBe("SimpleUser");
        }

        // Only the first of several, and only one of them is left.
        [Fact]
        public async Task A_member_has_one_role()
        {
            Seed.UserRole(_nikos, _member);
            Seed.UserRole(_nikos, _trainer);

            await SaveAsync(Admin, Form(x =>
            {
                x.Id = _nikos.Id.ToString();
                x.UserName = "nikos";
                x.Email = "nikos@gym.test";
                x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "Trainer" } }, new UserRoleDto { Role = new RoleDto { Name = "SimpleUser" } }];
            }));

            RolesOf(_nikos.Id).ShouldBe("Trainer");
        }

        // The role is given before the account is saved, and stays given when that is refused.
        [Fact]
        public async Task A_change_that_is_refused_has_already_given_the_role()
        {
            (await SaveAsync(Admin, Form(x =>
            {
                x.UserName = "nikos";
                x.FirstName = "Changed";
                x.UserRoles = [new UserRoleDto { Role = new RoleDto { Name = "Trainer" } }];
            }))).ShouldFail("Username 'nikos' is already taken.");

            Read().FirstName.ShouldBe("Maria");
            RolesOf().ShouldBe("Trainer");
        }

        [Fact]
        public async Task Two_members_cannot_have_one_address()
        {
            (await SaveAsync(Admin, Form(x => x.Email = "Nikos@Gym.Test"))).ShouldFail("Email 'Nikos@Gym.Test' is already taken.");

            Read().Email.ShouldBe("maria@gym.test");
        }

        [Fact]
        public async Task A_form_needs_its_fields()
        {
            (await SaveAsync(Admin, Form(x => { x.UserName = ""; x.Email = ""; x.FirstName = ""; x.LastName = ""; }))).ShouldBeBad(
                English.Required("UserName"),
                English.Required("Email"),
                English.Of(TranslationKeys.Invalid_email_address),
                English.Required("FirstName"),
                English.Required("LastName"));
        }


        // ================================================================
        // Whose form it is
        // ================================================================

        [Fact]
        public async Task The_address_and_the_form_have_to_agree()
        {
            string notFound = English.Of(TranslationKeys._0_not_found, "User");

            (await SaveAsync(Admin, Form(), _nikos.Id.ToString())).ShouldFail(notFound);
            (await SaveAsync(Admin, Form(x => x.Id = null), _maria.Id.ToString())).ShouldFail(notFound);
            // Letter for letter.
            (await SaveAsync(Admin, Form(), _maria.Id.ToString().ToUpperInvariant())).ShouldFail(notFound);
            (await SaveAsync(Admin, Form(x => x.Id = "x"))).ShouldFail(notFound);
            (await SaveAsync(Admin, Form(x => x.Id = Guid.NewGuid().ToString()))).ShouldFail(notFound);

            User gone = new User { Id = Guid.NewGuid() };
            (await SaveAsync(Caller.Of(gone), Form(x => x.Id = gone.Id.ToString()))).ShouldFail(notFound);
        }


        // ================================================================
        // Deleting one
        // ================================================================

        private Task<ApiResult<IdentityUser>> DeleteAsync(Caller caller, object id)
        {
            return DeleteAsync<IdentityUser>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task An_admin_deletes_a_member_and_what_is_theirs()
        {
            Seed.Plan(_maria);
            Seed.Subscription(_maria, 5);

            ApiResult<IdentityUser> result = await DeleteAsync(Admin, _maria.Id);

            result.ShouldSucceed(English.Of(TranslationKeys._0_deleted_successfully, "User maria@gym.test "));
            result.Data.ShouldBeNull();
            Database.Read(db => db.Users.Any(x => x.Id == _maria.Id)).ShouldBeFalse();
            Database.Read(db => db.WorkoutPlans.Count()).ShouldBe(0);
            Database.Read(db => db.Subscriptions.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Deleting_one_needs_the_delete_permission()
        {
            (await DeleteAsync(Caller.Of(_admin, "Users_View", "Users_Add", "Users_Edit"), _maria.Id)).ShouldFail(English.NotAuthorized);
            // Nor may they delete themselves.
            (await DeleteAsync(Caller.Of(_maria), _maria.Id)).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.Users.Any(x => x.Id == _maria.Id)).ShouldBeTrue();
        }

        [Fact]
        public async Task A_member_who_is_not_there_is_not_deleted()
        {
            (await DeleteAsync(Admin, Guid.NewGuid())).ShouldFail(English.Of(TranslationKeys._0_not_found, "User"));
            (await DeleteAsync(Admin, "x")).ShouldBreak();
        }

        // An admin may delete themselves.
        [Fact]
        public async Task An_admin_may_delete_themselves()
        {
            (await DeleteAsync(Admin, _admin.Id)).ShouldSucceed(English.Of(TranslationKeys._0_deleted_successfully, $"User {_admin.Email} "));
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // The address always has an id in it.
        [Fact]
        public async Task Without_an_id_there_is_nobody()
        {
            UsersController controller = Direct.Controller<UsersController>(Services, "Users", ["Users_View", "Users_Delete"]);
            string notFound = English.Of(TranslationKeys._0_not_found, "User");

            (await controller.Get(null)).Messages["error"].ShouldBe([notFound]);
            (await controller.Delete(null)).Messages["error"].ShouldBe([notFound]);
            (await controller.Delete("")).Messages["error"].ShouldBe([notFound]);
        }

        // Somebody else changed the account in between.
        [Fact]
        public async Task An_account_that_could_not_be_deleted_is_said_so()
        {
            UsersController controller = Direct.Controller<UsersController>(Services, "Users", ["Users_Delete"], null, ActivatorUtilities.CreateInstance<StubbornUserManager>(Services.CreateScope().ServiceProvider));

            (await controller.Delete(_maria.Id.ToString())).Messages["error"].ShouldBe(["Cannot be deleted."]);
        }

        private sealed class StubbornUserManager : UserManager<User>
        {
            public StubbornUserManager(
                IUserStore<User> store,
                IOptions<IdentityOptions> options,
                IPasswordHasher<User> hasher,
                IEnumerable<IUserValidator<User>> userValidators,
                IEnumerable<IPasswordValidator<User>> passwordValidators,
                ILookupNormalizer normalizer,
                IdentityErrorDescriber errors,
                IServiceProvider services,
                ILogger<UserManager<User>> logger) : base(store, options, hasher, userValidators, passwordValidators, normalizer, errors, services, logger)
            {
            }

            public override Task<IdentityResult> DeleteAsync(User user)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Cannot be deleted." }));
            }
        }

        // The account is gone in the moment between saving it and reading it back.
        [Fact]
        public async Task A_profile_that_cannot_be_read_back_is_the_one_that_was_saved()
        {
            IDataService data = ActivatorUtilities.CreateInstance<DataService>(Services.CreateScope().ServiceProvider, new NoUsers(Database.Factory));
            UsersController controller = Direct.Controller<UsersController>(Services, "Users", [], _maria, data);

            ApiResponse<UserDto> response = await controller.Update(_maria.Id.ToString(), Form(x => x.FirstName = "Changed"));

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldNotBeNull().FirstName.ShouldBe("Changed");
        }

        private sealed class NoUsers : GenericRepository<User>, IGenericRepository<User>
        {
            public NoUsers(IDbContextFactory<ApiDbContext> factory) : base(factory)
            {
            }

            public new GenericRepository<User> Include<TProperty>(Expression<Func<User, TProperty>> navigationProperty)
            {
                Where(x => false);

                return base.Include(navigationProperty);
            }

            GenericRepository<User> IGenericRepository<User>.Include<TProperty>(Expression<Func<User, TProperty>> navigationProperty)
            {
                return Include(navigationProperty);
            }
        }
    }
}
