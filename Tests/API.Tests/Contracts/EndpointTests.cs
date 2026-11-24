using System.Net;
using System.Reflection;
using System.Text;
using API.Tests.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared;

namespace API.Tests.Contracts
{
    /// <summary>
    /// Every address the client calls: that it is there, what it takes and gives back, and that it is
    /// shut to somebody who is not signed in.
    /// </summary>
    public sealed class EndpointTests : ApiTest
    {
        public EndpointTests(ApiFixture api) : base(api)
        {
        }

        private List<ControllerActionDescriptor> Actions()
        {
            return Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .OrderBy(x => x.AttributeRouteInfo!.Template, StringComparer.Ordinal)
                .ThenBy(VerbOf, StringComparer.Ordinal)
                .ToList();
        }

        private static string VerbOf(ControllerActionDescriptor action)
        {
            return action.ActionConstraints!.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().Single().HttpMethods.Single();
        }

        private static bool IsOpen(ControllerActionDescriptor action)
        {
            IList<object> metadata = action.EndpointMetadata;

            return metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any();
        }

        // A route that is renamed, moved or taken away breaks a page that nobody may open for weeks.
        // One that is added should be looked at once, for whom it is open to.
        [Fact]
        public void The_addresses_are_the_ones_that_were_approved()
        {
            StringBuilder text = new StringBuilder();

            foreach (ControllerActionDescriptor action in Actions())
            {
                string parameters = string.Join(", ", action.Parameters.Select(x => $"{Source(x.BindingInfo?.BindingSource)} {JsonShape.Name(x.ParameterType)} {x.Name}"));

                text.AppendLine($"{VerbOf(action)} {action.AttributeRouteInfo!.Template}{(IsOpen(action) ? "  OPEN TO ANYBODY" : "")}");
                if (parameters.Length > 0)
                    text.AppendLine($"  takes {parameters}");
                text.AppendLine($"  gives {JsonShape.Name(Answer(action.MethodInfo))}");
            }

            ApprovedFile.ShouldMatch(text.ToString(), "Endpoints");
        }

        private static string Source(BindingSource? source)
        {
            return source?.Id.ToLowerInvariant() ?? "anywhere";
        }

        // Without the wrappings of a controller's answer.
        internal static Type Answer(MethodInfo method)
        {
            Type type = method.ReturnType;

            while (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(Microsoft.AspNetCore.Mvc.ActionResult<>)))
                type = type.GetGenericArguments()[0];

            return type;
        }

        // Only those the list says are open answer somebody who is not signed in.
        [Fact]
        public async Task Somebody_who_is_not_signed_in_is_turned_away_everywhere_else()
        {
            int shut = 0;

            foreach (ControllerActionDescriptor action in Actions().Where(x => !IsOpen(x)))
            {
                string url = action.AttributeRouteInfo!.Template!.Replace("{id}", "1").Replace("{workoutPlanId}", "1");
                url.ShouldNotContain("{");

                using HttpResponseMessage response = await RawAsync(new HttpMethod(VerbOf(action)), Caller.Nobody, url, new { });

                response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{VerbOf(action)} {url}");
                shut++;
            }

            shut.ShouldBeGreaterThan(100);
        }

        // The same address and verb twice would be an error on every call.
        [Fact]
        public void No_two_are_the_same()
        {
            Actions().GroupBy(x => VerbOf(x) + " " + x.AttributeRouteInfo!.Template!.ToLowerInvariant()).Where(x => x.Count() > 1).Select(x => x.Key).ShouldBeEmpty();
        }

        // Besides the controllers there is the page every other address leads to, and nothing else.
        [Fact]
        public void There_is_nothing_besides_the_controllers_and_the_first_page()
        {
            IEnumerable<string?> others = Services.GetRequiredService<EndpointDataSource>().Endpoints
                .OfType<RouteEndpoint>()
                .Where(x => x.Metadata.GetMetadata<ControllerActionDescriptor>() == null)
                .Select(x => x.RoutePattern.RawText);

            others.ShouldBe(["{*path:nonfile}"]);
        }
    }
}
