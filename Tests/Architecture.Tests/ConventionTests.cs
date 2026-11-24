using System.Reflection;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Architecture.Tests
{
    /// <summary>
    /// What every controller, every table and every list of choices has in common. A new one that
    /// forgets it fails here.
    /// </summary>
    public sealed class ConventionTests
    {
        private static readonly Assembly Api = Assembly.Load("API");
        private static readonly Assembly CoreAssembly = Assembly.Load("Core");
        private static readonly Assembly DataAccessAssembly = Assembly.Load("DataAccess");
        private static readonly Assembly Business = Assembly.Load("Business");

        private static List<Type> Controllers()
        {
            List<Type> controllers = Api.GetTypes()
                .Where(x => typeof(ControllerBase).IsAssignableFrom(x) && !x.IsAbstract && !x.IsGenericTypeDefinition)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            controllers.Count.ShouldBeGreaterThan(20);

            return controllers;
        }


        // ================================================================
        // Controllers
        // ================================================================

        // Signing in comes before being signed in: there each action says for itself.
        [Fact]
        public void Every_controller_but_the_one_for_signing_in_is_shut()
        {
            Controllers().Where(x => !x.GetCustomAttributes<AuthorizeAttribute>(true).Any()).Select(x => x.Name).ShouldBe(["AuthController"]);
        }

        [Fact]
        public void No_action_of_the_others_is_open()
        {
            Controllers()
                .Where(x => x.Name != "AuthController")
                .SelectMany(x => x.GetMethods().Where(y => y.GetCustomAttributes<AllowAnonymousAttribute>(true).Any()).Select(y => $"{x.Name}.{y.Name}"))
                .ShouldBeEmpty();
        }

        [Fact]
        public void Every_controller_is_where_its_name_says()
        {
            foreach (Type controller in Controllers())
            {
                controller.Name.ShouldEndWith("Controller");
                controller.Namespace.ShouldBe("API.Controllers", controller.Name);
                controller.GetCustomAttributes<RouteAttribute>(true).First().Template.ShouldBe("api/[controller]", controller.Name);
            }
        }


        // ================================================================
        // Tables
        // ================================================================

        // A row of the app's own has an id and says when and by whom it was made.
        [Fact]
        public void Every_row_has_a_table()
        {
            List<Type> rows = CoreAssembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BaseModel))).ToList();
            List<Type> tables = typeof(ApiDbContext).GetProperties()
                .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(x => x.PropertyType.GetGenericArguments()[0])
                .ToList();

            rows.Count.ShouldBeGreaterThan(15);
            rows.Except(tables).ShouldBeEmpty();
        }

        [Fact]
        public void Every_configuration_is_where_its_name_says()
        {
            List<Type> configurations = DataAccessAssembly.GetTypes()
                .Where(x => x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
                .ToList();

            configurations.Count.ShouldBeGreaterThan(15);

            foreach (Type configuration in configurations)
            {
                configuration.Name.ShouldEndWith("Configuration");
                configuration.Namespace.ShouldBe("DataAccess.Configurations", configuration.Name);
            }
        }


        // ================================================================
        // Names
        // ================================================================

        [Fact]
        public void Every_list_of_choices_is_called_one()
        {
            List<Type> enums = CoreAssembly.GetTypes().Where(x => x.IsEnum).ToList();

            enums.Count.ShouldBeGreaterThan(4);
            enums.ShouldAllBe(x => x.Name.EndsWith("Enum"));

            // One is nowhere, and nothing uses it.
            enums.Where(x => x.Namespace != "Core.Enums").Select(x => x.Name).ShouldBe(["DayOfWeekEnum"]);
        }

        // With an I before it, and a class that does what it says.
        [Fact]
        public void Every_interface_of_a_service_is_named_and_kept()
        {
            List<Type> interfaces = Business.GetTypes().Where(x => x.IsInterface).ToList();

            interfaces.Count.ShouldBeGreaterThan(6);

            foreach (Type service in interfaces)
            {
                service.Name.ShouldStartWith("I");

                Business.GetTypes().Any(x => x.IsClass && x.GetInterfaces().Any(y => y == service || (y.IsGenericType && y.GetGenericTypeDefinition() == service))).ShouldBeTrue(service.Name);
            }
        }
    }
}
