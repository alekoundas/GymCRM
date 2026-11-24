using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace API.Tests.Host
{
    /// <summary>
    /// Who a request is from: the token the app itself would have given them, with the permissions
    /// the test is about and no others.
    /// </summary>
    public sealed class Caller
    {
        private readonly string? _token;

        private Caller(string? token)
        {
            _token = token;
        }

        /// <summary>
        /// Somebody who is not signed in.
        /// </summary>
        public static readonly Caller Nobody = new Caller(null);

        public static Caller Of(User user, params string[] permissions)
        {
            return new Caller(Token([new Claim("Id", user.Id.ToString()), new Claim("UserName", user.UserName ?? ""), .. Permissions(permissions)]));
        }

        /// <summary>
        /// Signed in with a token that does not say who they are.
        /// </summary>
        public static Caller WithNoId(params string[] permissions)
        {
            return new Caller(Token(Permissions(permissions)));
        }

        public static Caller WithClaims(params Claim[] claims)
        {
            return new Caller(Token(claims));
        }

        public static Caller WithToken(string token)
        {
            return new Caller(token);
        }

        public void Sign(HttpRequestMessage request)
        {
            if (_token != null)
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _token);
        }

        public static string Token(
            IEnumerable<Claim> claims,
            string issuer = Settings.Issuer,
            string audience = Settings.Issuer,
            string key = Settings.SecretKey,
            string algorithm = SecurityAlgorithms.HmacSha256,
            DateTime? expires = null,
            bool hasExpiry = true)
        {
            JwtSecurityToken token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: hasExpiry ? expires ?? DateTime.UtcNow.AddMinutes(10) : null,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static IEnumerable<Claim> Permissions(string[] permissions)
        {
            return permissions.Select(x => new Claim("Permission", x));
        }
    }
}
