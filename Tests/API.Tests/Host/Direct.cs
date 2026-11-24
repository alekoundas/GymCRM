using System.Security.Claims;
using Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Host
{
    /// <summary>
    /// A controller called without a request, for the few lines no request gets to: past what the
    /// json and the form's own rules let through, or with a service that does what the real one never
    /// does.
    /// </summary>
    internal static class Direct
    {
        /// <param name="name">What the controller's permissions begin with.</param>
        /// <param name="user">Who it is, or nobody.</param>
        /// <param name="given">Services to be used instead of the app's.</param>
        public static TController Controller<TController>(IServiceProvider services, string name, string[] permissions, User? user = null, params object[] given) where TController : ControllerBase
        {
            IServiceProvider provider = services.CreateScope().ServiceProvider;
            TController controller = ActivatorUtilities.CreateInstance<TController>(provider, given);

            List<Claim> claims = permissions.Select(x => new Claim("Permission", x)).ToList();
            if (user != null)
                claims.Add(new Claim("Id", user.Id.ToString()));

            DefaultHttpContext context = new DefaultHttpContext { RequestServices = provider, User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            controller.ControllerContext = new ControllerContext { HttpContext = context, ActionDescriptor = new ControllerActionDescriptor { ControllerName = name } };

            return controller;
        }
    }
}
