using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Business.Services.Email;
using Google.Apis.Gmail.v1;
using Google.Apis.Http;
using Google.Apis.Services;
using MimeKit;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Gmail's end of the line: takes the request the real client makes, and hands back the mail
    /// inside it. Nothing leaves the machine.
    /// </summary>
    internal sealed class StubGmail : HttpMessageHandler
    {
        public HttpStatusCode Answer { get; set; } = HttpStatusCode.OK;

        public List<string> Requests { get; } = new List<string>();

        public List<MimeMessage> Mails { get; } = new List<MimeMessage>();

        public List<string> Raw { get; } = new List<string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            string raw = JsonDocument.Parse(body).RootElement.GetProperty("raw").GetString()!;

            Requests.Add($"{request.Method} {request.RequestUri}");
            Raw.Add(raw);
            Mails.Add(Decode(raw));

            string answer = Answer == HttpStatusCode.OK ? """{ "id": "1" }""" : """{ "error": { "code": 400, "message": "Recipient address required" } }""";

            return new HttpResponseMessage(Answer) { Content = new StringContent(answer, Encoding.UTF8, "application/json"), RequestMessage = request };
        }

        /// <summary>
        /// Puts this in the place of the client the service would build for itself.
        /// </summary>
        public void PutInto(GmailEmailService service)
        {
            GmailService client = new GmailService(new BaseClientService.Initializer
            {
                HttpClientFactory = new Factory(this),
                ApplicationName = "Tests",
                GZipEnabled = false
            });

            ClientField.SetValue(service, client);
        }

        public static FieldInfo ClientField
        {
            get { return typeof(GmailEmailService).GetField("_gmailService", BindingFlags.NonPublic | BindingFlags.Instance)!; }
        }

        // Base64 as it is written in a url, without its padding.
        private static MimeMessage Decode(string raw)
        {
            string base64 = raw.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

            using MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64));

            return MimeMessage.Load(stream);
        }

        private sealed class Factory : HttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public Factory(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args)
            {
                return _handler;
            }
        }
    }
}
