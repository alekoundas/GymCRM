using Core.System;

namespace Core.Tests.System
{
    public sealed class TokenSettingsTests
    {
        [Fact]
        public void Settings_nobody_filled_in_are_empty_and_check_everything()
        {
            TokenSettings settings = new TokenSettings();

            settings.Issuer.ShouldBe("");
            settings.Audience.ShouldBe("");
            settings.SecretKey.ShouldBe("");
            settings.Issuers.ShouldBeEmpty();
            settings.Audiences.ShouldBeEmpty();
            settings.TokenExpireSeconds.ShouldBe(0);
            settings.RefreshTokenExpireSeconds.ShouldBe(0);
            settings.ValidateIssuer.ShouldBeTrue();
            settings.ValidateAudience.ShouldBeTrue();
            settings.ValidateLifetime.ShouldBeTrue();
        }

        [Fact]
        public void Settings_keep_what_they_are_given()
        {
            TokenSettings settings = new TokenSettings
            {
                Issuer = "issuer",
                Audience = "audience",
                SecretKey = "key",
                Issuers = ["one", "two"],
                Audiences = ["three"],
                TokenExpireSeconds = 20,
                RefreshTokenExpireSeconds = 30,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            settings.Issuer.ShouldBe("issuer");
            settings.Audience.ShouldBe("audience");
            settings.SecretKey.ShouldBe("key");
            settings.Issuers.ShouldBe(["one", "two"]);
            settings.Audiences.ShouldBe(["three"]);
            settings.TokenExpireSeconds.ShouldBe(20);
            settings.RefreshTokenExpireSeconds.ShouldBe(30);
            settings.ValidateIssuer.ShouldBeFalse();
            settings.ValidateAudience.ShouldBeFalse();
            settings.ValidateLifetime.ShouldBeFalse();
        }
    }
}
