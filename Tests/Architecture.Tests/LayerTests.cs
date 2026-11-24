using System.Reflection;

using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using SolutionArchitecture = ArchUnitNET.Domain.Architecture;

namespace Architecture.Tests
{
    /// <summary>
    /// Test:
    /// 1) Project references are correct between them.
    /// 2) Certain libraries are used only by a specific project.
    /// </summary>
    public sealed class LayerTests
    {
        private static readonly Assembly Core = Assembly.Load("Core");
        private static readonly Assembly DataAccess = Assembly.Load("DataAccess");
        private static readonly Assembly Business = Assembly.Load("Business");
        private static readonly Assembly Api = Assembly.Load("API");

        private static readonly SolutionArchitecture Solution = new ArchLoader()
            .LoadAssemblies(Core, DataAccess, Business, Api)
            .Build();

        private static List<string> UsersOf(string library)
        {
            return new[] { Core, DataAccess, Business, Api }
                .Where(x => x.GetReferencedAssemblies().Any(r => r.Name != null && (r.Name == library || r.Name.StartsWith(library + ".", StringComparison.Ordinal))))
                .Select(x => x.GetName().Name ?? string.Empty)
                .ToList();
        }


        // 1) Dependences - References
        [Fact]
        public void Core_depends_on_no_other_project()
        {
            Types().That().ResideInAssembly(Core)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(DataAccess, Business, Api))
                .Check(Solution);
        }

        [Fact]
        public void DataAccess_depends_on_Core_alone()
        {
            Types().That().ResideInAssembly(DataAccess)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(Business, Api))
                .Check(Solution);
        }

        [Fact]
        public void Business_never_touches_the_api()
        {
            Types().That().ResideInAssembly(Business)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(Api))
                .Check(Solution);
        }

        // A rule that looks at nothing would pass as well.
        [Fact]
        public void The_layers_that_are_looked_at_do_depend_on_each_other()
        {
            Types().That().HaveFullName("Business.Services.DataService")
                .Should().DependOnAny(Types().That().ResideInAssembly(DataAccess))
                .Check(Solution);

            Types().That().HaveFullName("API.Controllers.SubscriptionsController")
                .Should().DependOnAny(Types().That().ResideInAssembly(Business))
                .Check(Solution);
        }


        // 2) Libraries. The one project is in each list on purpose: a misspelt library would otherwise pass.
        [Theory]
        [InlineData("AutoMapper", "API")]
        [InlineData("Swashbuckle.AspNetCore", "API")]
        [InlineData("Microsoft.AspNetCore.Authentication.JwtBearer", "API")]
        [InlineData("Microsoft.AspNetCore.Mvc", "API")]
        [InlineData("Microsoft.EntityFrameworkCore.Sqlite", "DataAccess")]
        [InlineData("Microsoft.Data.Sqlite", "DataAccess")]
        [InlineData("Google.Apis", "Business")]
        [InlineData("MimeKit", "Business")]
        [InlineData("Ical.Net", "Business")]
        [InlineData("System.Linq.Dynamic.Core", "Business")]
        public void Each_library_has_its_project(string library, string project)
        {
            UsersOf(library).ShouldBe([project]);
        }

        [Fact]
        public void Core_uses_little_besides_the_framework()
        {
            string framework = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;

            List<string> outside = Core.GetReferencedAssemblies()
                .Where(x => !Path.GetDirectoryName(Assembly.Load(x).Location)!.Equals(framework, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Name ?? string.Empty)
                .Order()
                .ToList();

            // What an account is made of, and what a text is.
            outside.ShouldBe(["Microsoft.Extensions.Identity.Stores", "Microsoft.Extensions.Localization.Abstractions"]);
        }
    }
}
