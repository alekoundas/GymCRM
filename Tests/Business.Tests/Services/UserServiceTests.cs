using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Business.Services;
using Business.Tests.Fakes;
using Core.Dtos;
using Core.Models;
using Core.System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tests.Shared;

namespace Business.Tests.Services
{
    /// <summary>
    /// Signing in and out, and the two tokens a member carries.
    /// </summary>
    public sealed class UserServiceTests : IDisposable
    {
        private const string Password = "P@ssw0rd";
        private const string Key = "a-key-of-the-tests-own-and-long-enough-for-hmac-of-any-size-at-all";

        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly Clock _clock = new Clock();
        private readonly IdentityServices _identity;
        private readonly TokenSettings _settings;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _seed = new Seed(_database);
            _identity = new IdentityServices(_database);
            _settings = new TokenSettings
            {
                Issuer = "http://gym.test",
                Issuers = ["http://gym.test", "https://gym.test"],
                Audiences = ["http://gym.test", "https://gym.test"],
                SecretKey = Key,
                TokenExpireSeconds = 600,
                RefreshTokenExpireSeconds = 3600
            };
            _service = ServiceOn(_identity);
        }

        private UserService ServiceOn(IdentityServices identity)
        {
            RefreshTokenService refreshTokens = new RefreshTokenService(identity.Users, DataServices.On(_database), _clock);

            return new UserService(identity.Users, identity.Roles, refreshTokens, _settings, identity.SignIn);
        }

        public void Dispose()
        {
            _identity.Dispose();
            _database.Dispose();
        }

        // A member with the password, as Register makes one.
        private async Task<User> MemberAsync(string userName = "maria")
        {
            User user = new User { UserName = userName, Email = $"{userName}@gym.test", FirstName = "Maria", LastName = "Papa" };
            (await _identity.Users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();

            return user;
        }

        private async Task<Role> RoleAsync(string name, params string[] claims)
        {
            Role role = new Role { Name = name };
            (await _identity.Roles.CreateAsync(role)).Succeeded.ShouldBeTrue();

            foreach (string claim in claims)
                await _identity.Roles.AddClaimAsync(role, new Claim("Permission", claim));

            return role;
        }

        private User Read(User user)
        {
            return _database.Read(db => db.Users.Single(x => x.Id == user.Id));
        }

        private List<string?> RolesOf(User user)
        {
            return _database.Read(db => db.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.Role.Name).ToList());
        }


        // ================================================================
        // Signing in
        // ================================================================

        [Fact]
        public async Task The_right_password_signs_in()
        {
            (await _service.SignInUser(await MemberAsync(), Password)).ShouldBeNull();
        }

        [Theory]
        [InlineData("wrong")]
        [InlineData("p@ssw0rd")]
        [InlineData("P@ssw0rd ")]
        [InlineData("")]
        public async Task Any_other_password_does_not(string password)
        {
            (await _service.SignInUser(await MemberAsync(), password)).ShouldBe("Login unsuccessful!");
        }

        [Fact]
        public async Task A_member_with_no_password_cannot_sign_in()
        {
            User member = _seed.User();

            (await _service.SignInUser(await _identity.Users.FindByIdAsync(member.Id.ToString()) ?? member, Password)).ShouldBe("Login unsuccessful!");
        }

        [Fact]
        public async Task Each_wrong_password_is_counted_and_the_right_one_starts_over()
        {
            User member = await MemberAsync();

            for (int i = 1; i <= 4; i++)
            {
                await _service.SignInUser(member, "wrong");
                Read(member).AccessFailedCount.ShouldBe(i);
            }

            (await _service.SignInUser(member, Password)).ShouldBeNull();
            Read(member).AccessFailedCount.ShouldBe(0);
            Read(member).LockoutEnd.ShouldBeNull();
        }

        [Fact]
        public async Task The_fifth_wrong_password_locks_the_account_for_five_minutes()
        {
            User member = await MemberAsync();

            for (int i = 1; i <= 5; i++)
                await _service.SignInUser(member, "wrong");

            Read(member).LockoutEnd.ShouldNotBeNull().ShouldBe(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(30));
            (await _service.SignInUser(member, Password)).ShouldBe("Login unsuccessful!");
        }

        [Fact]
        public async Task A_lock_that_has_passed_no_longer_holds()
        {
            User member = await MemberAsync();
            await _identity.Users.SetLockoutEndDateAsync(member, DateTimeOffset.UtcNow.AddSeconds(-1));

            (await _service.SignInUser(member, Password)).ShouldBeNull();
        }

        [Fact]
        public async Task An_account_that_may_not_be_locked_is_not()
        {
            User member = await MemberAsync();
            await _identity.Users.SetLockoutEnabledAsync(member, false);

            for (int i = 1; i <= 6; i++)
                await _service.SignInUser(member, "wrong");

            (await _service.SignInUser(member, Password)).ShouldBeNull();
        }


        // ================================================================
        // Signing out
        // ================================================================

        [Fact]
        public async Task Signing_out_stamps_the_account_anew()
        {
            User member = await MemberAsync();
            string? before = Read(member).SecurityStamp;

            (await _service.SignOutUser(member)).ShouldBeNull();

            Read(member).SecurityStamp.ShouldNotBe(before);
        }

        [Fact]
        public async Task An_account_changed_meanwhile_cannot_be_signed_out()
        {
            User member = await MemberAsync();
            _database.Change(db => db.Users.Single(x => x.Id == member.Id).ConcurrencyStamp = "changed");

            (await _service.SignOutUser(member)).ShouldBe("Logout unsuccessful!");
        }

        [Fact]
        public async Task Signing_out_ends_the_sessions_of_every_device()
        {
            User maria = await MemberAsync("maria");
            User nikos = await MemberAsync("nikos");
            await _service.GenerateUserToken(maria);
            await _service.GenerateUserToken(maria);
            await _service.GenerateUserToken(nikos);

            await _service.SignOutUser(maria);

            StoredTokens().Select(x => x.UserId).ShouldBe([nikos.Id]);
        }

        [Fact]
        public async Task An_account_that_cannot_be_signed_out_keeps_its_sessions()
        {
            User member = await MemberAsync();
            UserLoginResponseDto tokens = await _service.GenerateUserToken(member);
            _database.Change(db => db.Users.Single(x => x.Id == member.Id).ConcurrencyStamp = "changed");

            await _service.SignOutUser(member);

            tokens.RefreshToken.ShouldStartWith(StoredTokens().Single().Name);
        }


        // ================================================================
        // A new member
        // ================================================================

        [Fact]
        public async Task A_new_member_is_written_with_a_hash_of_the_password()
        {
            User member = new User { UserName = "maria", Email = "maria@gym.test", PhoneNumbers = [new PhoneNumber { Number = "2101234567", IsPrimary = true }] };

            (await _service.AddNewUser(member, Password)).ShouldBeNull();

            User written = _database.Read(db => db.Users.Include(x => x.PhoneNumbers).Single());
            written.UserName.ShouldBe("maria");
            written.NormalizedUserName.ShouldBe("MARIA");
            written.NormalizedEmail.ShouldBe("MARIA@GYM.TEST");
            written.PasswordHash.ShouldNotBeNull().ShouldNotContain(Password);
            written.LockoutEnabled.ShouldBeTrue();
            written.PhoneNumbers.Single().Number.ShouldBe("2101234567");
        }

        [Theory]
        [InlineData("P@ss0rd", "Passwords must be at least 8 characters.")]
        [InlineData("Passw0rd", "Passwords must have at least one non alphanumeric character.")]
        [InlineData("p@ssw0rd", "Passwords must have at least one uppercase ('A'-'Z').")]
        [InlineData("P@SSW0RD", "Passwords must have at least one lowercase ('a'-'z').")]
        [InlineData("P@ssword", "Passwords must have at least one digit ('0'-'9').")]
        [InlineData("pass", "Passwords must be at least 8 characters., Passwords must have at least one non alphanumeric character., Passwords must have at least one digit ('0'-'9')., Passwords must have at least one uppercase ('A'-'Z').")]
        public async Task A_password_too_weak_is_refused_with_every_reason(string password, string reasons)
        {
            (await _service.AddNewUser(new User { UserName = "maria", Email = "maria@gym.test" }, password)).ShouldBe(reasons);

            _database.Read(db => db.Users.Count()).ShouldBe(0);
        }

        [Theory]
        [InlineData("maria", "other@gym.test", "Username 'maria' is already taken.")]
        [InlineData("MARIA", "other@gym.test", "Username 'MARIA' is already taken.")]
        [InlineData("other", "maria@gym.test", "Email 'maria@gym.test' is already taken.")]
        [InlineData("other", "MARIA@GYM.TEST", "Email 'MARIA@GYM.TEST' is already taken.")]
        [InlineData("maria", "maria@gym.test", "Username 'maria' is already taken., Email 'maria@gym.test' is already taken.")]
        // Letters of other alphabets and spaces are not for usernames.
        [InlineData("μαρία", "other@gym.test", "Username 'μαρία' is invalid, can only contain letters or digits.")]
        [InlineData("maria papa", "other@gym.test", "Username 'maria papa' is invalid, can only contain letters or digits.")]
        [InlineData("other", "not an email", "Email 'not an email' is invalid.")]
        [InlineData("", "other@gym.test", "Username '' is invalid, can only contain letters or digits.")]
        public async Task A_name_or_address_that_will_not_do_is_refused(string userName, string email, string reasons)
        {
            await MemberAsync("maria");

            (await _service.AddNewUser(new User { UserName = userName, Email = email }, Password)).ShouldBe(reasons);
        }


        // ================================================================
        // The one role
        // ================================================================

        [Theory]
        [InlineData("")]
        [InlineData("SimpleUser")]
        [InlineData("SimpleUser Trainer")]
        [InlineData("Administrator")]
        public async Task A_member_ends_up_with_the_one_role_whatever_they_had(string before)
        {
            foreach (string name in new[] { "SimpleUser", "Trainer", "Administrator" })
                await RoleAsync(name);
            User member = await MemberAsync();
            foreach (string name in before.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                await _identity.Users.AddToRoleAsync(member, name);

            await _service.AssignSingleRoleAsync(member, "Administrator");

            RolesOf(member).ShouldBe(["Administrator"]);
        }

        // By then the old one is gone, so the member is left with none.
        [Fact]
        public async Task A_role_that_is_not_there_throws_and_takes_the_old_one_along()
        {
            await RoleAsync("SimpleUser");
            User member = await MemberAsync();
            await _identity.Users.AddToRoleAsync(member, "SimpleUser");

            await Should.ThrowAsync<InvalidOperationException>(() => _service.AssignSingleRoleAsync(member, "Missing"));

            RolesOf(member).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_member_changed_meanwhile_is_not_given_the_role()
        {
            await RoleAsync("SimpleUser");
            User member = await MemberAsync();
            _database.Change(db => db.Users.Single(x => x.Id == member.Id).ConcurrencyStamp = "changed");

            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => _service.AssignSingleRoleAsync(member, "SimpleUser"));

            exception.Message.ShouldBe("Failed to assign role: Optimistic concurrency failure, object has been modified.");
            RolesOf(member).ShouldBeEmpty();
        }


        // ================================================================
        // The access token
        // ================================================================

        private static List<string> ClaimsOf(string token)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Select(x => $"{x.Type}={x.Value}").ToList();
        }

        private static readonly string NameClaim = ClaimTypes.Name;

        [Fact]
        public async Task A_token_says_who_they_are_and_what_they_may_do()
        {
            await RoleAsync("Trainer", "Mails_View", "Mails_Add");
            User member = await MemberAsync();
            await _identity.Users.AddToRoleAsync(member, "Trainer");
            await _identity.Users.AddClaimAsync(member, new Claim("Own", "claim"));
            DateTime before = DateTime.UtcNow.AddSeconds(-1);

            UserLoginResponseDto tokens = await _service.GenerateUserToken(member);

            List<string> claims = ClaimsOf(tokens.AccessToken).Where(x => !x.StartsWith("exp=", StringComparison.Ordinal)).ToList();
            claims.ShouldBe(
            [
                $"Id={member.Id}",
                "userName=maria",
                $"{NameClaim}=maria",
                "UserName=maria",
                "RoleName=Trainer",
                "Own=claim",
                "Permission=Mails_View",
                "Permission=Mails_Add",
                "aud=http://gym.test",
                "aud=https://gym.test",
                "iss=http://gym.test"
            ], ignoreOrder: true);

            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
            token.SignatureAlgorithm.ShouldBe("HS256");
            token.ValidTo.ShouldBeInRange(before.AddSeconds(599), DateTime.UtcNow.AddSeconds(601));

            // Filled in by the login itself.
            tokens.FirstName.ShouldBe("");
            tokens.LastName.ShouldBe("");
            tokens.ProfileImage.ShouldBeNull();
        }

        [Fact]
        public async Task A_member_with_no_role_may_do_nothing()
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            ClaimsOf(tokens.AccessToken).ShouldContain("RoleName=");
            ClaimsOf(tokens.AccessToken).Where(x => x.StartsWith("Permission=", StringComparison.Ordinal)).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_member_with_two_roles_may_do_what_either_allows_and_goes_by_the_first()
        {
            await RoleAsync("Trainer", "Mails_View");
            await RoleAsync("SimpleUser", "Subscriptions_View");
            User member = await MemberAsync();
            await _identity.Users.AddToRolesAsync(member, ["Trainer", "SimpleUser"]);

            List<string> claims = ClaimsOf((await _service.GenerateUserToken(member)).AccessToken);

            claims.Where(x => x.StartsWith("Permission=", StringComparison.Ordinal)).ShouldBe(["Permission=Mails_View", "Permission=Subscriptions_View"], ignoreOrder: true);
            claims.Count(x => x.StartsWith("RoleName=", StringComparison.Ordinal)).ShouldBe(1);
        }

        [Fact]
        public async Task A_role_that_cannot_be_found_adds_nothing()
        {
            await RoleAsync("Trainer", "Mails_View");
            User member = await MemberAsync();
            await _identity.Users.AddToRoleAsync(member, "Trainer");
            _identity.ForgottenRole = "Trainer";

            List<string> claims = ClaimsOf((await _service.GenerateUserToken(member)).AccessToken);

            claims.ShouldContain("RoleName=Trainer");
            claims.ShouldNotContain("Permission=Mails_View");
        }

        [Fact]
        public async Task A_member_with_no_username_gets_a_token_with_an_empty_one()
        {
            User member = await MemberAsync();
            member.UserName = null;

            List<string> claims = ClaimsOf((await _service.GenerateUserToken(member)).AccessToken);

            claims.ShouldContain("userName=");
            claims.ShouldContain("UserName=");
            claims.ShouldContain($"{NameClaim}=");
        }

        [Fact]
        public async Task An_audience_of_its_own_is_one_more()
        {
            _settings.Audience = "http://one-more.test";

            ClaimsOf((await _service.GenerateUserToken(await MemberAsync())).AccessToken).Where(x => x.StartsWith("aud=", StringComparison.Ordinal))
                .ShouldBe(["aud=http://gym.test", "aud=https://gym.test", "aud=http://one-more.test"], ignoreOrder: true);
        }

        [Fact]
        public async Task A_key_too_short_to_sign_with_throws()
        {
            _settings.SecretKey = "short";

            await Should.ThrowAsync<ArgumentOutOfRangeException>(async () => await _service.GenerateUserToken(await MemberAsync()));
        }


        // ================================================================
        // The refresh token
        // ================================================================

        private List<IdentityUserToken<Guid>> StoredTokens()
        {
            return _database.Read(db => db.UserTokens.ToList());
        }

        // What a session is and when it ends is in RefreshTokenServiceTests.
        [Fact]
        public async Task Every_sign_in_begins_a_session()
        {
            User maria = await MemberAsync("maria");
            User nikos = await MemberAsync("nikos");

            UserLoginResponseDto first = await _service.GenerateUserToken(maria);
            UserLoginResponseDto second = await _service.GenerateUserToken(maria);
            UserLoginResponseDto other = await _service.GenerateUserToken(nikos);

            StoredTokens().Select(x => (x.UserId, x.Name)).ShouldBe(
            [
                (maria.Id, first.RefreshToken.Split('.')[0]),
                (maria.Id, second.RefreshToken.Split('.')[0]),
                (nikos.Id, other.RefreshToken.Split('.')[0])
            ], ignoreOrder: true);
        }


        // ================================================================
        // Going on without signing in again
        // ================================================================

        private static string Error(ApiResponse<UserRefreshResponseDto> response)
        {
            response.IsSucceed.ShouldBeFalse();
            response.Data.ShouldBeNull();

            return response.Messages["error"].Single();
        }

        private Task<ApiResponse<UserRefreshResponseDto>> RefreshAsync(string accessToken, string refreshToken)
        {
            return _service.RefreshToken(new UserRefreshRequestDto { AccessToken = accessToken, RefreshToken = refreshToken });
        }

        // A token of any content, signed with the key there is or with another.
        private string Token(string? userName = "maria", string issuer = "http://gym.test", string audience = "http://gym.test", string key = Key, string algorithm = SecurityAlgorithms.HmacSha256, int expiresIn = 600)
        {
            List<Claim> claims = userName == null ? [] : [new Claim(ClaimTypes.Name, userName)];
            SigningCredentials credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                notBefore: DateTime.UtcNow.AddDays(-2),
                expires: DateTime.UtcNow.AddSeconds(expiresIn),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task Both_tokens_together_get_two_new_ones()
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            ApiResponse<UserRefreshResponseDto> response = await RefreshAsync(tokens.AccessToken, tokens.RefreshToken);

            response.IsSucceed.ShouldBeTrue();
            response.Messages.ShouldBeEmpty();
            ClaimsOf(response.Data.ShouldNotBeNull().AccessToken).ShouldContain("UserName=maria");
            response.Data.RefreshToken.ShouldNotBe(tokens.RefreshToken);
            StoredTokens().Count.ShouldBe(1);
        }

        [Fact]
        public async Task The_new_access_token_says_what_they_may_do_by_now()
        {
            await RoleAsync("Trainer", "Mails_View");
            User member = await MemberAsync();
            UserLoginResponseDto tokens = await _service.GenerateUserToken(member);
            await _identity.Users.AddToRoleAsync(member, "Trainer");

            ApiResponse<UserRefreshResponseDto> response = await RefreshAsync(tokens.AccessToken, tokens.RefreshToken);

            ClaimsOf(tokens.AccessToken).ShouldNotContain("Permission=Mails_View");
            ClaimsOf(response.Data.ShouldNotBeNull().AccessToken).ShouldContain("Permission=Mails_View");
        }

        // That is what it is for.
        [Theory]
        [InlineData(-1)]
        [InlineData(-60 * 60 * 24)]
        public async Task An_access_token_that_has_expired_will_do(int expiresIn)
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            (await RefreshAsync(Token(expiresIn: expiresIn), tokens.RefreshToken)).IsSucceed.ShouldBeTrue();
        }

        [Theory]
        [InlineData("http://gym.test", "https://gym.test")]
        [InlineData("https://gym.test", "http://gym.test")]
        public async Task Any_of_the_gyms_addresses_will_do(string issuer, string audience)
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            (await RefreshAsync(Token(issuer: issuer, audience: audience), tokens.RefreshToken)).IsSucceed.ShouldBeTrue();
        }

        [Fact]
        public async Task A_username_is_found_however_it_is_typed()
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            (await RefreshAsync(Token(userName: "MARIA"), tokens.RefreshToken)).IsSucceed.ShouldBeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nobody")]
        public async Task A_token_of_nobody_is_refused(string? userName)
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            Error(await RefreshAsync(Token(userName: userName), tokens.RefreshToken)).ShouldBe("User not found");
        }

        [Theory]
        [InlineData("")]
        [InlineData("wrong")]
        public async Task A_wrong_refresh_token_is_refused(string refreshToken)
        {
            UserLoginResponseDto tokens = await _service.GenerateUserToken(await MemberAsync());

            Error(await RefreshAsync(tokens.AccessToken, refreshToken)).ShouldBe("Please Login again.");
        }

        [Fact]
        public async Task Somebody_elses_refresh_token_is_refused()
        {
            UserLoginResponseDto maria = await _service.GenerateUserToken(await MemberAsync("maria"));
            UserLoginResponseDto nikos = await _service.GenerateUserToken(await MemberAsync("nikos"));

            Error(await RefreshAsync(maria.AccessToken, nikos.RefreshToken)).ShouldBe("Please Login again.");
        }

        [Fact]
        public async Task Signing_out_ends_the_refresh_token()
        {
            User member = await MemberAsync();
            UserLoginResponseDto tokens = await _service.GenerateUserToken(member);
            await _service.SignOutUser(member);

            Error(await RefreshAsync(tokens.AccessToken, tokens.RefreshToken)).ShouldBe("Please Login again.");
        }

        [Fact]
        public async Task A_sign_in_on_another_device_leaves_this_one_as_it_is()
        {
            User member = await MemberAsync();
            UserLoginResponseDto phone = await _service.GenerateUserToken(member);
            UserLoginResponseDto laptop = await _service.GenerateUserToken(member);

            (await RefreshAsync(phone.AccessToken, phone.RefreshToken)).IsSucceed.ShouldBeTrue();
            (await RefreshAsync(laptop.AccessToken, laptop.RefreshToken)).IsSucceed.ShouldBeTrue();
        }

        private async Task<UserRefreshResponseDto> RefreshedAsync(string accessToken, string refreshToken)
        {
            _clock.Pass(TimeSpan.FromMinutes(1));

            return (await RefreshAsync(accessToken, refreshToken)).Data.ShouldNotBeNull();
        }

        [Fact]
        public async Task A_refresh_token_that_was_replaced_twice_is_refused()
        {
            UserLoginResponseDto first = await _service.GenerateUserToken(await MemberAsync());
            UserRefreshResponseDto second = await RefreshedAsync(first.AccessToken, first.RefreshToken);
            UserRefreshResponseDto third = await RefreshedAsync(second.AccessToken, second.RefreshToken);

            Error(await RefreshAsync(first.AccessToken, first.RefreshToken)).ShouldBe("Please Login again.");

            // And so is the one that took its place: nobody can tell which of them is the member's.
            Error(await RefreshAsync(third.AccessToken, third.RefreshToken)).ShouldBe("Please Login again.");
        }

        [Fact]
        public async Task A_refresh_token_from_before_there_were_sessions_will_do()
        {
            User member = await MemberAsync();
            string old = await _identity.Users.GenerateUserTokenAsync(member, "REFRESHTOKENPROVIDER", "RefreshToken");

            (await RefreshAsync(Token(), old)).IsSucceed.ShouldBeTrue();
        }

        [Fact]
        public async Task A_refresh_token_that_has_run_out_is_refused()
        {
            using IdentityServices shortLived = new IdentityServices(_database, refreshTokenSeconds: -1);
            UserService service = ServiceOn(shortLived);
            User member = await MemberAsync();
            UserLoginResponseDto tokens = await service.GenerateUserToken((await shortLived.Users.FindByNameAsync("maria")).ShouldNotBeNull());

            ApiResponse<UserRefreshResponseDto> response = await service.RefreshToken(new UserRefreshRequestDto { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken });

            Error(response).ShouldBe("Please Login again.");
            member.Id.ShouldNotBe(Guid.Empty);
        }


        // An access token that is not ours is not answered at all: it throws.

        [Fact]
        public async Task A_token_signed_with_another_key_throws()
        {
            await Should.ThrowAsync<SecurityTokenSignatureKeyNotFoundException>(() => RefreshAsync(Token(key: "another-key-that-is-long-enough-to-sign-with-it"), "x"));
        }

        [Fact]
        public async Task A_token_that_was_changed_throws()
        {
            string[] parts = Token().Split('.');
            string other = Token(userName: "nikos").Split('.')[1];

            await Should.ThrowAsync<SecurityTokenException>(() => RefreshAsync($"{parts[0]}.{other}.{parts[2]}", "x"));
        }

        [Fact]
        public async Task A_token_from_somewhere_else_throws()
        {
            await Should.ThrowAsync<SecurityTokenInvalidIssuerException>(() => RefreshAsync(Token(issuer: "http://elsewhere.test"), "x"));
        }

        [Fact]
        public async Task A_token_for_somewhere_else_throws()
        {
            await Should.ThrowAsync<SecurityTokenInvalidAudienceException>(() => RefreshAsync(Token(audience: "http://elsewhere.test"), "x"));
        }

        // The right key, another way of signing.
        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha384)]
        [InlineData(SecurityAlgorithms.HmacSha512)]
        public async Task A_token_signed_another_way_throws(string algorithm)
        {
            (await Should.ThrowAsync<SecurityTokenException>(() => RefreshAsync(Token(algorithm: algorithm), "x"))).Message
                .ShouldBe("GetPrincipalFromExpiredToken Token is not validated");
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a token")]
        [InlineData("a.b.c")]
        public async Task Something_that_is_not_a_token_throws(string accessToken)
        {
            await Should.ThrowAsync<ArgumentException>(() => RefreshAsync(accessToken, "x"));
        }
    }
}
