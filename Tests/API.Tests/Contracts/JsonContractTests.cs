using System.Text.Json;
using API.Tests.Host;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tests.Shared;

namespace API.Tests.Contracts
{
    /// <summary>
    /// The json between the client and the server, property by property. The client has a type of its
    /// own for each of these, written by hand: a name that changes here is undefined there, and nothing
    /// says so.
    /// </summary>
    public sealed class JsonContractTests : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _api;

        public JsonContractTests(ApiFixture api)
        {
            _api = api;
        }

        private JsonSerializerOptions Options
        {
            get { return _api.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions; }
        }

        private List<ControllerActionDescriptor> Actions()
        {
            return _api.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
        }

        [Fact]
        public void What_the_client_sends_is_what_was_approved()
        {
            IEnumerable<Type> types = Actions().SelectMany(x => x.Parameters).Where(x => x.BindingInfo?.BindingSource == BindingSource.Body).Select(x => x.ParameterType);

            ApprovedFile.ShouldMatch(JsonShape.Describe(types, Options), "Requests");
        }

        [Fact]
        public void What_the_client_is_sent_is_what_was_approved()
        {
            IEnumerable<Type> types = Actions().Select(x => EndpointTests.Answer(x.MethodInfo));

            ApprovedFile.ShouldMatch(JsonShape.Describe(types, Options), "Answers");
        }

        [Fact]
        public void The_json_is_written_the_way_the_client_reads_it()
        {
            Options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
            Options.PropertyNameCaseInsensitive.ShouldBeTrue();
            Options.DefaultIgnoreCondition.ShouldBe(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
            Options.ReferenceHandler.ShouldBe(System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
            Options.Converters.Select(x => x.GetType().Name).ShouldBe(["JsonStringEnumConverter", "JsonDateTimeConverter"]);
            Options.WriteIndented.ShouldBeFalse();
            Options.MaxDepth.ShouldBe(32);
        }

        // As they are named, and not in small letters as properties are.
        [Fact]
        public void The_names_of_a_dictionary_stay_as_they_are()
        {
            JsonSerializer.Serialize(new Dictionary<string, int> { ["Key_Name"] = 1 }, Options).ShouldBe("{\"Key_Name\":1}");
        }
    }
}
