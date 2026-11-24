using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Tests.Host
{
    /// <summary>
    /// An answer as the client reads it: the status, then isSucceed, messages and data.
    /// </summary>
    public sealed class ApiResult<T>
    {
        public HttpStatusCode Status { get; init; }

        public bool IsSucceed { get; init; }

        public Dictionary<string, string[]> Messages { get; init; } = new Dictionary<string, string[]>();

        public T? Data { get; init; }

        /// <summary>
        /// The answer as it came.
        /// </summary>
        public string Body { get; init; } = "";

        /// <summary>
        /// The data, for a test that cannot go on without it.
        /// </summary>
        public T Value
        {
            get
            {
                IsSucceed.ShouldBeTrue(Body);

                return Data ?? throw new InvalidOperationException("There is no data in " + Body);
            }
        }

        public void ShouldSucceed(string? message = null)
        {
            Status.ShouldBe(HttpStatusCode.OK, Body);
            IsSucceed.ShouldBeTrue(Body);

            if (message == null)
                Messages.ShouldBeEmpty();
            else
                Messages.ShouldHaveSingleItem().ShouldSatisfyAllConditions(x => x.Key.ShouldBe("success"), x => x.Value.ShouldBe([message]));
        }

        /// <summary>
        /// Answered, and the answer is no.
        /// </summary>
        public void ShouldFail(params string[] errors)
        {
            Status.ShouldBe(HttpStatusCode.OK, Body);
            ShouldCarry(errors);
        }

        /// <summary>
        /// Refused as a bad request, with the reasons.
        /// </summary>
        public void ShouldBeBad(params string[] errors)
        {
            Status.ShouldBe(HttpStatusCode.BadRequest, Body);
            ShouldCarry(errors);
        }

        /// <summary>
        /// The app threw: the client gets no answer it can read.
        /// </summary>
        public void ShouldBreak()
        {
            Status.ShouldBe(HttpStatusCode.InternalServerError, Body);
            Body.ShouldBeEmpty();
        }

        private void ShouldCarry(string[] errors)
        {
            IsSucceed.ShouldBeFalse(Body);
            Data.ShouldBe(default, Body);
            Messages.ShouldHaveSingleItem().Key.ShouldBe("error");

            // ASP.NET Core lists what is wrong with a form by the length of the field's name.
            Messages["error"].ShouldBe(errors, ignoreOrder: true);
        }
    }

    public static class ApiResult
    {
        // What the client's own code does with the answer, give or take.
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public static ApiResult<T> Read<T>(HttpStatusCode status, string body)
        {
            if (!body.StartsWith('{'))
                return new ApiResult<T> { Status = status, Body = body };

            using JsonDocument json = JsonDocument.Parse(body);
            JsonElement root = json.RootElement;

            return new ApiResult<T>
            {
                Status = status,
                Body = body,
                IsSucceed = root.TryGetProperty("isSucceed", out JsonElement isSucceed) && isSucceed.GetBoolean(),
                Messages = root.TryGetProperty("messages", out JsonElement messages) ? messages.Deserialize<Dictionary<string, string[]>>()! : [],
                Data = root.TryGetProperty("data", out JsonElement data) ? data.Deserialize<T>(Json) : default
            };
        }
    }
}
