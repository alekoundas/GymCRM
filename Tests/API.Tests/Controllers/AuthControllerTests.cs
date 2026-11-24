using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using API.Controllers;
using API.Tests.Host;
using Business.Services;
using Core.Dtos;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Controllers
{
    public sealed class AuthControllerTests : ApiTest
    {
        private const string Password = "P@ssw0rd";
        private const string NewPassword = "N3w!Passw0rd";

        public AuthControllerTests(ApiFixture api) : base(api)
        {
        }

        private async Task<User> AccountAsync(string userName = "maria", Action<User>? change = null)
        {
            using IServiceScope scope = Services.CreateScope();

            User user = new User { UserName = userName, Email = $"{userName}@gym.test", FirstName = "Maria", LastName = "Papa" };
            change?.Invoke(user);
            (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().CreateAsync(user, Password)).Succeeded.ShouldBeTrue();

            return user;
        }

        private Task<ApiResult<UserLoginResponseDto>> LoginAsync(string name, string password)
        {
            return PostAsync<UserLoginResponseDto>(Caller.Nobody, "api/Auth/Login", new { userNameOrEmail = name, password });
        }

        private User Read(User user)
        {
            return Database.Read(db => db.Users.Single(x => x.Id == user.Id));
        }

        // A link or a refresh token as the app makes them, of any age.
        private string Signed(string provider, User user, string purpose, TimeSpan age)
        {
            using MemoryStream content = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(content, new UTF8Encoding(false, true), leaveOpen: true))
            {
                writer.Write((DateTimeOffset.UtcNow - age).UtcTicks);
                writer.Write(user.Id.ToString());
                writer.Write(purpose);
                writer.Write(Read(user).SecurityStamp.ShouldNotBeNull());
            }

            return Convert.ToBase64String(Services.GetRequiredService<IDataProtectionProvider>().CreateProtector(provider).Protect(content.ToArray()));
        }


        // ================================================================
        // Signing in
        // ================================================================

        [Theory]
        [InlineData("maria")]
        [InlineData("MARIA")]
        [InlineData("maria@gym.test")]
        [InlineData("Maria@Gym.Test")]
        public async Task A_member_signs_in_by_name_or_by_address(string name)
        {
            User user = await AccountAsync(change: x => x.ProfileImage = [1, 2, 3]);

            ApiResult<UserLoginResponseDto> result = await LoginAsync(name, Password);

            result.ShouldSucceed();
            result.Value.FirstName.ShouldBe("Maria");
            result.Value.LastName.ShouldBe("Papa");
            result.Value.ProfileImage.ShouldBe([1, 2, 3]);
            result.Value.RefreshToken.ShouldNotBeEmpty();
            new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken).Claims.Single(x => x.Type == "Id").Value.ShouldBe(user.Id.ToString());
        }

        [Fact]
        public async Task The_token_that_comes_back_opens_the_app()
        {
            await AccountAsync();
            string token = (await LoginAsync("maria", Password)).Value.AccessToken;

            (await PostAsync<bool>(Caller.WithToken(token), "api/Auth/Logout", null)).ShouldSucceed();
        }

        [Theory]
        [InlineData("nobody")]
        [InlineData("nobody@gym.test")]
        [InlineData("")]
        public async Task Nobody_we_know_cannot_sign_in(string name)
        {
            await AccountAsync();

            (await LoginAsync(name, Password)).ShouldFail(English.Of(TranslationKeys.User_Name_Email_not_found));
        }

        [Theory]
        [InlineData("wrong")]
        [InlineData("")]
        public async Task The_wrong_password_does_not_sign_in(string password)
        {
            User user = await AccountAsync();

            (await LoginAsync("maria", password)).ShouldFail("Login unsuccessful!");
            Read(user).AccessFailedCount.ShouldBe(1);
        }

        [Fact]
        public async Task Five_wrong_passwords_lock_the_account()
        {
            await AccountAsync();

            for (int i = 0; i < 5; i++)
                (await LoginAsync("maria", "wrong")).ShouldFail("Login unsuccessful!");

            (await LoginAsync("maria", Password)).ShouldFail("Login unsuccessful!");
        }

        [Fact]
        public async Task A_sign_in_with_nothing_filled_in_is_refused()
        {
            (await PostAsync<UserLoginResponseDto>(Caller.Nobody, "api/Auth/Login", new { userNameOrEmail = (string?)null, password = (string?)null }, "en"))
                .Status.ShouldBe(HttpStatusCode.BadRequest);
        }


        // ================================================================
        // Signing out
        // ================================================================

        [Fact]
        public async Task Signing_out_stamps_the_account_anew()
        {
            User user = await AccountAsync();
            string? before = Read(user).SecurityStamp;

            ApiResult<bool> result = await PostAsync<bool>(Caller.Of(user), "api/Auth/Logout", null);

            result.ShouldSucceed();
            result.Data.ShouldBeTrue();
            Read(user).SecurityStamp.ShouldNotBe(before);
        }

        [Fact]
        public async Task An_account_that_is_gone_cannot_be_signed_out()
        {
            User gone = new User { Id = Guid.NewGuid(), UserName = "gone" };

            (await PostAsync<bool>(Caller.Of(gone), "api/Auth/Logout", null)).ShouldFail(English.Of(TranslationKeys._0_not_found, "User"));
        }

        [Fact]
        public async Task A_token_with_no_username_breaks_signing_out()
        {
            (await PostAsync<bool>(Caller.WithNoId(), "api/Auth/Logout", null)).ShouldBreak();
        }


        // ================================================================
        // Going on without signing in again
        // ================================================================

        private Task<ApiResult<UserRefreshResponseDto>> RefreshAsync(string accessToken, string refreshToken)
        {
            return PostAsync<UserRefreshResponseDto>(Caller.Nobody, "api/Auth/RefreshToken", new { accessToken, refreshToken });
        }

        [Fact]
        public async Task Both_tokens_get_two_new_ones()
        {
            await AccountAsync();
            UserLoginResponseDto tokens = (await LoginAsync("maria", Password)).Value;

            ApiResult<UserRefreshResponseDto> result = await RefreshAsync(tokens.AccessToken, tokens.RefreshToken);

            result.ShouldSucceed();
            result.Value.AccessToken.ShouldNotBeEmpty();
            result.Value.RefreshToken.ShouldNotBe(tokens.RefreshToken);
        }

        [Fact]
        public async Task A_wrong_refresh_token_has_to_sign_in_again()
        {
            await AccountAsync();
            UserLoginResponseDto tokens = (await LoginAsync("maria", Password)).Value;

            (await RefreshAsync(tokens.AccessToken, "wrong")).ShouldFail("Please Login again.");
        }

        // As if the last token of every session had been made a while ago.
        private void Age(TimeSpan age)
        {
            Database.Change(db =>
            {
                foreach (IdentityUserToken<Guid> session in db.UserTokens)
                    session.Value = $"{session.Value!.Split(' ')[0]} {(DateTime.UtcNow - age).Ticks}";
            });
        }

        [Fact]
        public async Task A_refresh_token_is_good_until_the_one_that_follows_it_is_used()
        {
            await AccountAsync();
            UserLoginResponseDto first = (await LoginAsync("maria", Password)).Value;

            Age(TimeSpan.FromMinutes(2));
            UserRefreshResponseDto second = (await RefreshAsync(first.AccessToken, first.RefreshToken)).Value;

            Age(TimeSpan.FromMinutes(2));
            (await RefreshAsync(first.AccessToken, first.RefreshToken)).ShouldSucceed();
            UserRefreshResponseDto third = (await RefreshAsync(second.AccessToken, second.RefreshToken)).Value;

            (await RefreshAsync(first.AccessToken, first.RefreshToken)).ShouldFail("Please Login again.");
            // There were two copies of the session, and it is over for both.
            (await RefreshAsync(third.AccessToken, third.RefreshToken)).ShouldFail("Please Login again.");
            Database.Read(db => db.UserTokens.Count()).ShouldBe(0);
        }

        // Three tabs that open together, or an answer that was lost.
        [Fact]
        public async Task The_same_refresh_token_may_be_sent_more_than_once()
        {
            await AccountAsync();
            UserLoginResponseDto tokens = (await LoginAsync("maria", Password)).Value;
            Age(TimeSpan.FromMinutes(2));

            ApiResult<UserRefreshResponseDto>[] results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => RefreshAsync(tokens.AccessToken, tokens.RefreshToken)));

            foreach (ApiResult<UserRefreshResponseDto> result in results)
                (await RefreshAsync(result.Value.AccessToken, result.Value.RefreshToken)).ShouldSucceed();
        }

        [Fact]
        public async Task Each_device_goes_on_by_itself()
        {
            await AccountAsync();
            UserLoginResponseDto phone = (await LoginAsync("maria", Password)).Value;
            UserLoginResponseDto laptop = (await LoginAsync("maria", Password)).Value;

            for (int i = 0; i < 3; i++)
            {
                Age(TimeSpan.FromMinutes(2));
                UserRefreshResponseDto next = (await RefreshAsync(phone.AccessToken, phone.RefreshToken)).Value;
                (phone.AccessToken, phone.RefreshToken) = (next.AccessToken, next.RefreshToken);
            }

            (await RefreshAsync(laptop.AccessToken, laptop.RefreshToken)).ShouldSucceed();
        }

        [Fact]
        public async Task Signing_out_on_one_device_ends_them_all()
        {
            User user = await AccountAsync();
            UserLoginResponseDto phone = (await LoginAsync("maria", Password)).Value;
            UserLoginResponseDto laptop = (await LoginAsync("maria", Password)).Value;

            await PostAsync<bool>(Caller.Of(user), "api/Auth/Logout", null);

            (await RefreshAsync(phone.AccessToken, phone.RefreshToken)).ShouldFail("Please Login again.");
            (await RefreshAsync(laptop.AccessToken, laptop.RefreshToken)).ShouldFail("Please Login again.");
            Database.Read(db => db.UserTokens.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Another_password_ends_every_session()
        {
            User user = await AccountAsync();
            UserLoginResponseDto tokens = (await LoginAsync("maria", Password)).Value;

            (await ChangeAsync(Caller.Of(user), user.Id.ToString(), Password, NewPassword, NewPassword)).ShouldSucceed(English.Of(TranslationKeys.Password_changed_successfully));

            (await RefreshAsync(tokens.AccessToken, tokens.RefreshToken)).ShouldFail("Please Login again.");
        }

        // An hour, in the tests' settings.
        [Theory]
        [InlineData(59, true)]
        [InlineData(61, false)]
        public async Task A_refresh_token_lives_as_long_as_the_settings_say(int minutes, bool isGood)
        {
            User user = await AccountAsync();
            UserLoginResponseDto tokens = (await LoginAsync("maria", Password)).Value;
            string session = tokens.RefreshToken.Split('.')[0];
            string old = Signed("REFRESHTOKENPROVIDER", user, $"RefreshToken {session} 1", TimeSpan.FromMinutes(minutes));

            ApiResult<UserRefreshResponseDto> result = await RefreshAsync(tokens.AccessToken, $"{session}.1.{old}");

            if (isGood)
                result.ShouldSucceed();
            else
                result.ShouldFail("Please Login again.");
        }

        // Nobody is signed out by the release that brings the sessions.
        [Theory]
        [InlineData(59, true)]
        [InlineData(61, false)]
        public async Task A_refresh_token_from_before_there_were_sessions_lives_out_its_time(int minutes, bool isGood)
        {
            User user = await AccountAsync();
            string accessToken = (await LoginAsync("maria", Password)).Value.AccessToken;
            string old = Signed("REFRESHTOKENPROVIDER", user, "RefreshToken", TimeSpan.FromMinutes(minutes));

            ApiResult<UserRefreshResponseDto> result = await RefreshAsync(accessToken, old);

            if (isGood)
                (await RefreshAsync(result.Value.AccessToken, result.Value.RefreshToken)).ShouldSucceed();
            else
                result.ShouldFail("Please Login again.");
        }

        [Fact]
        public async Task A_password_link_is_no_refresh_token()
        {
            await AccountAsync();
            string accessToken = (await LoginAsync("maria", Password)).Value.AccessToken;
            await ForgotAsync("maria@gym.test");

            (await RefreshAsync(accessToken, TokenInMail())).ShouldFail("Please Login again.");
        }

        [Fact]
        public async Task A_token_of_nobody_we_know_is_refused()
        {
            (await RefreshAsync(Caller.Token([new Claim(ClaimTypes.Name, "nobody")]), "any")).ShouldFail("User not found");
            (await RefreshAsync(Caller.Token([]), "any")).ShouldFail("User not found");
        }

        [Fact]
        public async Task A_token_that_is_missing_is_refused()
        {
            await AccountAsync();
            string accessToken = (await LoginAsync("maria", Password)).Value.AccessToken;

            (await PostAsync<UserRefreshResponseDto>(Caller.Nobody, "api/Auth/RefreshToken", new { accessToken, refreshToken = (string?)null })).Status.ShouldBe(HttpStatusCode.BadRequest);
            (await PostAsync<UserRefreshResponseDto>(Caller.Nobody, "api/Auth/RefreshToken", new { accessToken = (string?)null, refreshToken = "any" })).Status.ShouldBe(HttpStatusCode.BadRequest);
            (await RefreshAsync(accessToken, "")).ShouldFail("Please Login again.");
            // Left out altogether, it is an empty one.
            (await PostAsync<UserRefreshResponseDto>(Caller.Nobody, "api/Auth/RefreshToken", new { accessToken })).ShouldFail("Please Login again.");
        }

        [Fact]
        public async Task Something_that_is_not_a_token_breaks()
        {
            (await RefreshAsync("not a token", "any")).ShouldBreak();
            (await RefreshAsync("", "")).ShouldBreak();
        }


        // ================================================================
        // A forgotten password
        // ================================================================

        private Task<ApiResult<bool>> ForgotAsync(string email, string? language = null)
        {
            return PostAsync<bool>(Caller.Nobody, "api/Auth/PasswordForgot", new { email }, language);
        }

        // The token of the link in the last mail.
        private string TokenInMail()
        {
            string link = Regex.Match(Emails.Bodies.Last(), "href=\"([^\"]+)\"").Groups[1].Value;

            return Uri.UnescapeDataString(link[(link.IndexOf("&token=", StringComparison.Ordinal) + "&token=".Length)..]);
        }

        [Fact]
        public async Task A_member_who_forgot_is_sent_a_link_there_and_then()
        {
            await AccountAsync("maria", x => x.Email = "maria+gym@gym.test");

            (await ForgotAsync("maria+gym@gym.test")).ShouldSucceed(English.Of(TranslationKeys.Password_reset_email_sent));

            Emails.Lines.ShouldBe(["send maria+gym@gym.test: Reset Your Password"]);
            Emails.Bodies.Single().ShouldContain("<a href=\"http://gym.test/users/reset-password?email=maria%2Bgym%40gym.test&token=");
            Emails.Bodies.Single().ShouldContain("This link expires in 24 hours.");
            TokenInMail().ShouldNotBeEmpty();
        }

        // The answer is the same, so nobody can find out who is a member.
        [Theory]
        [InlineData("nobody@gym.test")]
        [InlineData("maria")]
        public async Task Somebody_we_do_not_know_gets_the_same_answer_and_no_mail(string email)
        {
            await AccountAsync();

            (await ForgotAsync(email)).ShouldSucceed(English.Of(TranslationKeys.Password_reset_email_sent));

            Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task The_mail_goes_to_the_address_as_it_was_typed()
        {
            await AccountAsync();

            await ForgotAsync("MARIA@gym.test");

            Emails.Lines.ShouldBe(["send MARIA@gym.test: Reset Your Password"]);
        }

        [Fact]
        public async Task No_address_is_refused()
        {
            (await ForgotAsync("")).ShouldBeBad(English.Required("Email"));
        }

        // The answer follows the language; the mail does not.
        [Fact]
        public async Task The_mail_is_in_english_whatever_the_language()
        {
            await AccountAsync();

            (await ForgotAsync("maria@gym.test", "el")).Messages["success"].Single().ShouldNotBe(English.Of(TranslationKeys.Password_reset_email_sent));

            Emails.Lines.ShouldBe(["send maria@gym.test: Reset Your Password"]);
        }


        // ================================================================
        // A new password by the link
        // ================================================================

        private Task<ApiResult<bool>> ResetAsync(string email, string token, string newPassword, string confirm = "")
        {
            return PostAsync<bool>(Caller.Nobody, "api/Auth/PasswordReset", new { email, token, newPassword, confirmNewPassword = confirm });
        }

        [Fact]
        public async Task The_link_sets_a_new_password()
        {
            await AccountAsync();
            await ForgotAsync("maria@gym.test");

            (await ResetAsync("maria@gym.test", TokenInMail(), NewPassword, NewPassword)).ShouldSucceed("Password changed successfully.");

            (await LoginAsync("maria", NewPassword)).ShouldSucceed();
            (await LoginAsync("maria", Password)).ShouldFail("Login unsuccessful!");
        }

        // The second field is for the page to compare; nothing is done with it here.
        [Fact]
        public async Task The_password_repeated_wrongly_is_set_all_the_same()
        {
            await AccountAsync();
            await ForgotAsync("maria@gym.test");

            (await ResetAsync("maria@gym.test", TokenInMail(), NewPassword, "something else")).ShouldSucceed("Password changed successfully.");
        }

        [Fact]
        public async Task The_link_works_once()
        {
            await AccountAsync();
            await ForgotAsync("maria@gym.test");
            string token = TokenInMail();
            await ResetAsync("maria@gym.test", token, NewPassword);

            (await ResetAsync("maria@gym.test", token, Password)).ShouldFail(English.Of(TranslationKeys.Password_reset_failed_0, "Invalid token."));
        }

        // As its mail says.
        [Theory]
        [InlineData(0, true)]
        [InlineData(24 * 60 - 1, true)]
        [InlineData(24 * 60 + 1, false)]
        [InlineData(7 * 24 * 60 - 1, false)]
        public async Task The_link_works_for_a_day(int minutes, bool isGood)
        {
            User user = await AccountAsync();
            string token = Signed("Default", user, "ResetPassword", TimeSpan.FromMinutes(minutes));

            ApiResult<bool> result = await ResetAsync("maria@gym.test", token, NewPassword);

            if (isGood)
                result.ShouldSucceed("Password changed successfully.");
            else
                result.ShouldFail(English.Of(TranslationKeys.Password_reset_failed_0, "Invalid token."));
        }

        // It is signed as what it is, and a refresh token as another thing.
        [Fact]
        public async Task A_refresh_token_is_no_link()
        {
            User user = await AccountAsync();
            string refreshToken = (await LoginAsync("maria", Password)).Value.RefreshToken;

            foreach (string token in new[] { refreshToken, refreshToken.Split('.')[2], Signed("REFRESHTOKENPROVIDER", user, "ResetPassword", TimeSpan.Zero) })
                (await ResetAsync("maria@gym.test", token, NewPassword)).ShouldFail(English.Of(TranslationKeys.Password_reset_failed_0, "Invalid token."));
        }

        [Fact]
        public async Task The_link_of_somebody_else_does_not_work()
        {
            await AccountAsync("maria");
            await AccountAsync("nikos");
            await ForgotAsync("nikos@gym.test");

            (await ResetAsync("maria@gym.test", TokenInMail(), NewPassword)).ShouldFail(English.Of(TranslationKeys.Password_reset_failed_0, "Invalid token."));
        }

        [Fact]
        public async Task A_link_that_is_none_does_not_work()
        {
            await AccountAsync();

            (await ResetAsync("maria@gym.test", "wrong", NewPassword)).ShouldFail(English.Of(TranslationKeys.Password_reset_failed_0, "Invalid token."));
        }

        [Fact]
        public async Task An_address_nobody_has_is_refused()
        {
            (await ResetAsync("nobody@gym.test", "any", NewPassword)).ShouldFail(English.Of(TranslationKeys.Invalid_email));
        }

        [Fact]
        public async Task A_password_too_weak_is_refused_with_every_reason()
        {
            await AccountAsync();
            await ForgotAsync("maria@gym.test");

            (await ResetAsync("maria@gym.test", TokenInMail(), "password")).ShouldFail(English.Of(
                TranslationKeys.Password_reset_failed_0,
                "Passwords must have at least one non alphanumeric character., Passwords must have at least one digit ('0'-'9')., Passwords must have at least one uppercase ('A'-'Z')."));
        }

        [Fact]
        public async Task A_reset_with_something_missing_is_refused()
        {
            (await ResetAsync("", "", "short")).ShouldBeBad(English.Required("Email"), English.Required("Token"), English.Of(TranslationKeys.New_password_must_be_at_least_6_characters));
        }


        // ================================================================
        // Another password
        // ================================================================

        private Task<ApiResult<bool>> ChangeAsync(Caller caller, string userId, string oldPassword, string newPassword, string confirm)
        {
            return PostAsync<bool>(caller, "api/Auth/PasswordChange", new { userId, oldPassword, newPassword, confirmNewPassword = confirm });
        }

        [Fact]
        public async Task A_member_changes_their_password()
        {
            User user = await AccountAsync();

            (await ChangeAsync(Caller.Of(user), user.Id.ToString(), Password, NewPassword, NewPassword)).ShouldSucceed(English.Of(TranslationKeys.Password_changed_successfully));

            (await LoginAsync("maria", NewPassword)).ShouldSucceed();
        }

        // Whatever they know and whatever they may do. Nor can the old password be guessed at.
        [Theory]
        [InlineData(Password)]
        [InlineData("wrong")]
        public async Task Nobody_changes_somebody_elses(string oldPassword)
        {
            User user = await AccountAsync();
            User other = Seed.User();

            foreach (Caller caller in new[] { Caller.Of(other), Caller.Of(other, "Users_View", "Users_Add", "Users_Edit", "Users_Delete", "Roles_Edit"), Caller.WithNoId() })
                (await ChangeAsync(caller, user.Id.ToString(), oldPassword, NewPassword, NewPassword)).ShouldFail(English.NotAuthorized);

            (await LoginAsync("maria", Password)).ShouldSucceed();
        }

        // Nor is it found out who there is.
        [Theory]
        [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("not an id")]
        public async Task Somebody_who_is_not_there_is_answered_the_same(string userId)
        {
            (await ChangeAsync(Caller.Of(Seed.User()), userId, Password, NewPassword, NewPassword)).ShouldFail(English.NotAuthorized);
            (await ChangeAsync(Caller.WithNoId(), userId, Password, NewPassword, NewPassword)).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task An_account_that_is_gone_is_not_found()
        {
            User gone = new User { Id = Guid.NewGuid(), UserName = "gone" };

            (await ChangeAsync(Caller.Of(gone), gone.Id.ToString(), Password, NewPassword, NewPassword)).ShouldFail(English.Of(TranslationKeys._0_not_found, "User"));
        }

        [Theory]
        [InlineData("N")]
        [InlineData("B")]
        [InlineData("P")]
        public async Task An_id_is_one_however_it_is_written(string format)
        {
            User user = await AccountAsync();

            (await ChangeAsync(Caller.Of(user), user.Id.ToString(format).ToUpperInvariant(), Password, NewPassword, NewPassword)).ShouldSucceed(English.Of(TranslationKeys.Password_changed_successfully));
        }

        [Theory]
        // The old one is looked at first, then whether the new ones are the same, then how strong.
        [InlineData("wrong", "password", "other", "Old password is incorrect.")]
        [InlineData(Password, "password", "other", "Passwords dont match.")]
        [InlineData(Password, NewPassword, "", "Passwords dont match.")]
        [InlineData(Password, "password", "password", "Passwords must have at least one non alphanumeric character., Passwords must have at least one digit ('0'-'9')., Passwords must have at least one uppercase ('A'-'Z').")]
        public async Task A_change_that_will_not_do_is_refused(string oldPassword, string newPassword, string confirm, string error)
        {
            User user = await AccountAsync();

            (await ChangeAsync(Caller.Of(user), user.Id.ToString(), oldPassword, newPassword, confirm)).ShouldFail(error);

            (await LoginAsync("maria", Password)).ShouldSucceed();
        }

        [Fact]
        public async Task A_change_with_something_missing_is_refused()
        {
            (await ChangeAsync(Caller.Of(Seed.User()), "", "", "short", "")).ShouldBeBad(
                English.Required("UserId"), English.Required("OldPassword"), English.Of(TranslationKeys.New_password_must_be_at_least_6_characters));
        }


        // ================================================================
        // A new member
        // ================================================================

        private static Dictionary<string, object?> Form()
        {
            return new Dictionary<string, object?>
            {
                ["userName"] = "maria",
                ["email"] = "maria@gym.test",
                ["firstName"] = "Μαρία",
                ["lastName"] = "Παπά",
                ["address"] = "Athens",
                ["medicalHistory"] = "Knee",
                ["password"] = Password,
                ["phoneNumbers"] = new[] { new { number = "2101234567", isPrimary = true, userId = Guid.Empty } }
            };
        }

        private Task<ApiResult<bool>> RegisterAsync(Dictionary<string, object?> form)
        {
            return PostAsync<bool>(Caller.Nobody, "api/Auth/Register", form);
        }

        [Fact]
        public async Task A_new_member_is_written_with_their_numbers_and_the_members_role()
        {
            ApiResult<bool> result = await RegisterAsync(Form());

            result.ShouldSucceed();
            result.Data.ShouldBeTrue();

            User user = Database.Read(db => db.Users.Include(x => x.PhoneNumbers).Include(x => x.UserRoles).ThenInclude(x => x.Role).Single());
            user.UserName.ShouldBe("maria");
            user.Email.ShouldBe("maria@gym.test");
            user.FirstName.ShouldBe("Μαρία");
            user.LastName.ShouldBe("Παπά");
            user.PhoneNumbers.Single().Number.ShouldBe("2101234567");
            user.PhoneNumbers.Single().IsPrimary.ShouldBeTrue();
            user.UserRoles.Single().Role.Name.ShouldBe("SimpleUser");
            (await LoginAsync("maria", Password)).ShouldSucceed();
        }

        // The form asks for both, and neither is kept.
        [Fact]
        public async Task The_address_and_the_medical_history_are_not_kept()
        {
            await RegisterAsync(Form());

            User user = Database.Read(db => db.Users.Single());
            user.Address.ShouldBe("");
            user.MedicalHistory.ShouldBe("");
        }

        [Fact]
        public async Task The_members_role_is_made_when_it_is_missing_and_used_when_it_is_not()
        {
            await RegisterAsync(Form());
            Role made = Database.Read(db => db.Roles.Single());
            Dictionary<string, object?> second = Form();
            second["userName"] = "nikos";
            second["email"] = "nikos@gym.test";

            await RegisterAsync(second);

            made.NormalizedName.ShouldBe("SIMPLEUSER");
            Database.Read(db => db.Roles.Single()).Id.ShouldBe(made.Id);
            Database.Read(db => db.UserRoles.Count(x => x.RoleId == made.Id)).ShouldBe(2);
        }

        [Fact]
        public async Task A_name_and_address_somebody_has_are_refused()
        {
            await RegisterAsync(Form());

            (await RegisterAsync(Form())).ShouldFail("Username 'maria' is already taken., Email 'maria@gym.test' is already taken.");

            Database.Read(db => db.Users.Count()).ShouldBe(1);
        }

        [Fact]
        public async Task A_password_too_weak_is_refused()
        {
            Dictionary<string, object?> form = Form();
            form["password"] = "";

            (await RegisterAsync(form)).IsSucceed.ShouldBeFalse();
            Database.Read(db => db.Users.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Everything_that_is_wrong_with_the_form_is_said_at_once()
        {
            Dictionary<string, object?> form = Form();
            form["userName"] = "";
            form["email"] = "not an email";
            form["firstName"] = new string('x', 51);
            form["lastName"] = " ";
            form["address"] = new string('x', 101);
            form["phoneNumbers"] = Array.Empty<object>();

            ApiResult<bool> result = await RegisterAsync(form);

            result.Status.ShouldBe(HttpStatusCode.BadRequest);
            result.Messages["error"].ShouldBe(
            [
                English.Required("UserName"),
                English.Of(TranslationKeys.Invalid_email_address),
                English.Of(TranslationKeys.FirstName_cannot_exceed_50_characters),
                English.Required("LastName"),
                English.Of(TranslationKeys.Address_cannot_exceed_200_characters),
                English.Of(TranslationKeys.At_least_1_Train_Group_Date_is_required)
            ], ignoreOrder: true);
        }

        // The rule has a message of its own, about phone numbers, and the one for train groups is sent.
        [Fact]
        public async Task A_member_with_no_number_is_told_about_train_groups()
        {
            Dictionary<string, object?> form = Form();
            form["phoneNumbers"] = Array.Empty<object>();

            (await RegisterAsync(form)).ShouldBeBad(English.Of(TranslationKeys.At_least_1_Train_Group_Date_is_required));
        }

        [Theory]
        [InlineData("not a number", "", "Invalid_phone_number_format", "_0_is_required")]
        [InlineData("2101234567", "", null, "_0_is_required")]
        [InlineData("", "00000000-0000-0000-0000-000000000000", "_0_is_required", null)]
        public async Task A_number_that_will_not_do_is_refused(string number, string userId, string? numberError, string? userError)
        {
            Dictionary<string, object?> form = Form();
            form["phoneNumbers"] = new[] { new { number, isPrimary = true, userId } };

            List<string> errors = [];
            if (numberError != null)
                errors.Add(English.Of(numberError, "Number"));
            if (userError != null)
                errors.Add(English.Of(userError, "UserId"));

            ApiResult<bool> result = await RegisterAsync(form);

            result.Status.ShouldBe(HttpStatusCode.BadRequest);
            foreach (string error in errors)
                result.Messages["error"].ShouldContain(error);
        }

        // The page sends the empty id; anything that is no id at all cannot be read.
        [Fact]
        public async Task A_number_of_somebody_who_is_no_id_breaks()
        {
            Dictionary<string, object?> form = Form();
            form["phoneNumbers"] = new[] { new { number = "2101234567", isPrimary = true, userId = "not an id" } };

            (await RegisterAsync(form)).ShouldBreak();
            Database.Read(db => db.Users.Count()).ShouldBe(0);
        }


        // ================================================================
        // The gym's own Gmail
        // ================================================================

        private static readonly User Administrator = new User { Id = Guid.NewGuid(), UserName = "administrator" };

        private static readonly Caller Admin = Caller.Of(Administrator, "Roles_Edit");

        // What the app sends along to Google, and wants to see again.
        private async Task<string> StateAsync(Caller caller)
        {
            string url = (await GetAsync<string>(caller, "api/Auth/google")).Value;

            return Uri.UnescapeDataString(Regex.Match(url, "&state=([^&]+)&").Groups[1].Value);
        }

        [Fact]
        public async Task Whoever_may_edit_roles_is_sent_to_google()
        {
            ApiResult<string> result = await GetAsync<string>(Admin, "api/Auth/google");

            result.ShouldSucceed("");
            Regex.Replace(result.Value, "&state=[^&]+&", "&state=the state&").ShouldBe(
                "https://accounts.google.com/o/oauth2/v2/auth?client_id=client%20id&redirect_uri=http%3A%2F%2Fgym.test%2Fauth%2Fgoogle%2Fcallback" +
                "&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fgmail.send&response_type=code&state=the state&access_type=offline&prompt=consent");
        }

        // The gym's mail leaves through whichever account is connected.
        [Theory]
        [InlineData("")]
        [InlineData("Roles_View Roles_Add Roles_Delete")]
        [InlineData("Mails_View Mails_Add Mails_Edit Mails_Delete")]
        [InlineData("roles_edit")]
        public async Task Connecting_an_account_takes_the_permission_and_no_other_will_do(string permissions)
        {
            Caller caller = Caller.Of(Administrator, permissions.Split(' '));
            string state = await StateAsync(Admin);

            (await GetAsync<string>(caller, "api/Auth/google")).ShouldFail(English.NotAuthorized);
            (await CallbackAsync(caller, "x", state)).ShouldFail(English.NotAuthorized);

            Google.Requests.ShouldBeEmpty();
        }

        [Fact]
        public async Task Somebody_who_is_not_signed_in_or_not_anybody_cannot_connect_one()
        {
            string state = await StateAsync(Admin);

            (await GetAsync<string>(Caller.Nobody, "api/Auth/google")).Status.ShouldBe(HttpStatusCode.Unauthorized);
            (await CallbackAsync(Caller.Nobody, "x", state)).Status.ShouldBe(HttpStatusCode.Unauthorized);
            (await GetAsync<string>(Caller.WithNoId("Roles_Edit"), "api/Auth/google")).ShouldFail(English.NotAuthorized);
            (await CallbackAsync(Caller.WithNoId("Roles_Edit"), "x", state)).ShouldFail(English.NotAuthorized);

            Google.Requests.ShouldBeEmpty();
        }

        private Task<ApiResult<bool>> CallbackAsync(Caller caller, string code, string state)
        {
            return CallbackAsync(caller, $"?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}");
        }

        private Task<ApiResult<bool>> CallbackAsync(Caller caller, string query)
        {
            return GetAsync<bool>(caller, "api/Auth/googlecallback" + query);
        }

        // Back from Google, as it should be.
        private async Task<ApiResult<bool>> BackAsync()
        {
            return await CallbackAsync(Admin, "the code", await StateAsync(Admin));
        }

        private List<string> StoredTokens()
        {
            return Database.Read(db => db.GoogleRefreshTokens.OrderBy(x => x.Id).Select(x => x.RefreshToken + " " + x.ExpiresIn).ToList());
        }

        [Fact]
        public async Task The_code_google_sends_back_is_exchanged_for_a_token_that_is_kept()
        {
            ApiResult<bool> result = await BackAsync();

            result.ShouldSucceed("");
            result.Data.ShouldBeFalse();
            Google.Requests.ShouldBe(
            [
                "POST https://oauth2.googleapis.com/token client_id=client+id&client_secret=client+secret&code=the+code&grant_type=authorization_code" +
                "&redirect_uri=http%3A%2F%2Fgym.test%2Fauth%2Fgoogle%2Fcallback"
            ]);
            StoredTokens().ShouldBe(["refresh 3599"]);
        }

        [Fact]
        public async Task A_second_token_takes_the_place_of_the_first()
        {
            await BackAsync();
            Google.Answer = """{ "refresh_token": "newer", "expires_in": 10 }""";

            (await BackAsync()).ShouldSucceed("");

            StoredTokens().ShouldBe(["newer 10"]);
        }

        [Theory]
        [InlineData("wrong")]
        // What it was for everybody, once.
        [InlineData("random-state-for-csrf")]
        [InlineData("???")]
        [InlineData("ΑΒΓ")]
        public async Task An_answer_to_a_question_we_did_not_ask_is_refused(string state)
        {
            (await CallbackAsync(Admin, "x", state)).ShouldFail("Invalid state");

            Google.Requests.ShouldBeEmpty();
            StoredTokens().ShouldBeEmpty();
        }

        // A link somebody else sends an administrator connects nothing, whoever it is from.
        [Fact]
        public async Task Nor_is_one_somebody_else_was_sent_off_with()
        {
            Caller other = Caller.Of(new User { Id = Guid.NewGuid(), UserName = "other" }, "Roles_Edit");
            string state = await StateAsync(other);

            (await CallbackAsync(Admin, "x", state)).ShouldFail("Invalid state");
            (await CallbackAsync(other, "x", state)).ShouldSucceed("");
        }

        [Fact]
        public async Task Nor_is_one_that_was_changed()
        {
            string state = await StateAsync(Admin);
            int middle = state.Length / 2;
            string changed = state[..middle] + (state[middle] == 'A' ? 'B' : 'A') + state[(middle + 1)..];

            (await CallbackAsync(Admin, "x", changed)).ShouldFail("Invalid state");
            (await CallbackAsync(Admin, "x", state + state)).ShouldFail("Invalid state");
        }

        private ITimeLimitedDataProtector States()
        {
            return Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Auth.GoogleState").ToTimeLimitedDataProtector();
        }

        [Fact]
        public async Task There_are_ten_minutes_to_come_back()
        {
            string id = States().Unprotect(await StateAsync(Admin), out DateTimeOffset until);

            id.ShouldBe(Administrator.Id.ToString());
            until.ShouldBe(DateTimeOffset.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(30));
        }

        [Theory]
        [InlineData(30, true)]
        [InlineData(-1, false)]
        public async Task An_answer_that_took_longer_is_refused(int secondsLeft, bool isGood)
        {
            string state = States().Protect(Administrator.Id.ToString(), TimeSpan.FromSeconds(secondsLeft));

            ApiResult<bool> result = await CallbackAsync(Admin, "x", state);

            if (isGood)
                result.ShouldSucceed("");
            else
                result.ShouldFail("Invalid state");
        }

        // Something else the app signed for them.
        [Fact]
        public async Task An_answer_signed_for_something_else_is_refused()
        {
            string state = Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Something else").ToTimeLimitedDataProtector()
                .Protect(Administrator.Id.ToString(), TimeSpan.FromMinutes(10));

            (await CallbackAsync(Admin, "x", state)).ShouldFail("Invalid state");
        }

        [Theory]
        [InlineData("", "The code field is required.", "The state field is required.")]
        [InlineData("?state=x", "The code field is required.", null)]
        [InlineData("?code=x", "The state field is required.", null)]
        public async Task An_answer_with_something_missing_is_refused(string query, string first, string? second)
        {
            (await CallbackAsync(Admin, query)).ShouldBeBad(second == null ? [first] : [first, second]);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task What_google_says_no_with_is_passed_on(HttpStatusCode status)
        {
            Google.Status = status;
            Google.Answer = "invalid_grant";

            (await BackAsync()).ShouldFail("Token exchange failed: invalid_grant");

            StoredTokens().ShouldBeEmpty();
        }

        // Google leaves the token out when the account had already agreed.
        [Theory]
        [InlineData("{}")]
        [InlineData("null")]
        [InlineData("""{ "access_token": "access", "expires_in": 10 }""")]
        [InlineData("not json")]
        public async Task An_answer_with_no_token_breaks_and_keeps_the_one_there_was(string answer)
        {
            Google.Answer = answer;
            (await BackAsync()).ShouldBreak();
            StoredTokens().ShouldBeEmpty();

            Database.Add(new GoogleRefreshToken { RefreshToken = "kept", ExpiresIn = 1 });
            (await BackAsync()).ShouldBreak();
            StoredTokens().ShouldBe(["kept 1"]);
        }

        [Fact]
        public void What_google_answers_is_read_under_googles_own_names()
        {
            GoogleTokens tokens = new GoogleTokens { access_token = "a", refresh_token = "b", expires_in = 1, token_type = "c" };

            (tokens.access_token, tokens.refresh_token, tokens.expires_in, tokens.token_type).ShouldBe(("a", "b", 1, "c"));
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // The controller by itself, with a say in what it is given.
        private AuthController Alone(ClaimsPrincipal user, params object[] given)
        {
            AuthController controller = ActivatorUtilities.CreateInstance<AuthController>(Services.CreateScope().ServiceProvider, given);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            return controller;
        }

        // A request without a token is turned away before it gets here.
        [Fact]
        public async Task Somebody_who_is_not_signed_in_cannot_sign_out()
        {
            (await Alone(new ClaimsPrincipal()).Logout()).Messages["error"].ShouldBe([English.NotSignedIn]);
            (await Alone(new ClaimsPrincipal(new ClaimsIdentity())).Logout()).Messages["error"].ShouldBe([English.NotSignedIn]);
        }

        [Fact]
        public async Task A_sign_out_that_fails_says_why()
        {
            User user = await AccountAsync();
            ClaimsPrincipal signedIn = new ClaimsPrincipal(new ClaimsIdentity([new Claim("UserName", user.UserName!)], "test"));

            ApiResponse<bool> response = await Alone(signedIn, new FakeUserService("Logout unsuccessful!")).Logout();

            response.IsSucceed.ShouldBeFalse();
            response.Messages["error"].ShouldBe(["Logout unsuccessful!"]);
        }

        [Fact]
        public void Without_a_client_there_is_nowhere_to_sign_in()
        {
            IConfiguration empty = new ConfigurationBuilder().Build();
            ClaimsPrincipal administrator = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Id", Administrator.Id.ToString()), new Claim("Permission", "Roles_Edit")], "test"));

            Should.Throw<ArgumentNullException>(() => Alone(administrator, empty).StartGoogleAuth());
        }

        private sealed class FakeUserService : IUserService
        {
            private readonly string? _signOutError;

            public FakeUserService(string? signOutError)
            {
                _signOutError = signOutError;
            }

            public Task<string?> SignOutUser(User user)
            {
                return Task.FromResult(_signOutError);
            }

            public Task<string?> SignInUser(User user, string password)
            {
                throw new NotSupportedException();
            }

            public Task<string?> AddNewUser(User user, string password)
            {
                throw new NotSupportedException();
            }

            public Task AssignSingleRoleAsync(User user, string roleName)
            {
                throw new NotSupportedException();
            }

            public Task<UserLoginResponseDto> GenerateUserToken(User user)
            {
                throw new NotSupportedException();
            }

            public Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request)
            {
                throw new NotSupportedException();
            }
        }
    }
}
