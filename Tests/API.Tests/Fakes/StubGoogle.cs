using System.Net;

namespace API.Tests.Fakes
{
    /// <summary>
    /// Google's end of the sign-in, answering whatever it is told to.
    /// </summary>
    public sealed class StubGoogle : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; }

        public string Answer { get; set; } = "";

        /// <summary>
        /// Each request as "POST http://address body".
        /// </summary>
        public List<string> Requests { get; } = new List<string>();

        public void Reset()
        {
            Status = HttpStatusCode.OK;
            Answer = """{ "access_token": "access", "refresh_token": "refresh", "expires_in": 3599, "token_type": "Bearer" }""";
            Requests.Clear();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add($"{request.Method} {request.RequestUri} {body}");

            return new HttpResponseMessage(Status) { Content = new StringContent(Answer), RequestMessage = request };
        }
    }
}
