using Tests.Shared;

namespace API.Tests.Host
{
    /// <summary>
    /// The settings a test starts the app with. Every one that names something outside the machine -
    /// the database, the key, Google - is the test's own.
    /// </summary>
    internal static class Settings
    {
        public const string Issuer = "http://gym.test";
        public const string OtherIssuer = "https://gym.test";
        public const string SecretKey = "a-key-of-the-tests-own-and-long-enough-for-hmac-of-any-size-at-all";

        public static List<string> For(TestDatabase database, string environment = "Production")
        {
            return
            [
                "--environment=" + environment,
                "--applicationName=API",
                "--contentRoot=" + AppContext.BaseDirectory,
                "--urls=http://127.0.0.1:0",
                "--ConnectionStrings:DefaultConnection=" + database.ConnectionString,
                "--TokenSettings:Issuer=" + Issuer,
                "--TokenSettings:Issuers:0=" + Issuer,
                "--TokenSettings:Issuers:1=" + OtherIssuer,
                "--TokenSettings:Audiences:0=" + Issuer,
                "--TokenSettings:Audiences:1=" + OtherIssuer,
                "--TokenSettings:SecretKey=" + SecretKey,
                "--TokenSettings:TokenExpireSeconds=600",
                "--TokenSettings:RefreshTokenExpireSeconds=3600",
                "--Gmail:Email=gym@gym.test",
                "--Gmail:FromName=Gym",
                "--Gmail:ClientId=client id",
                "--Gmail:ClientSecret=client secret",
                "--Gmail:RefreshToken=",
                "--Gmail:RedirectUri=http://gym.test/auth/google/callback"
            ];
        }
    }
}
