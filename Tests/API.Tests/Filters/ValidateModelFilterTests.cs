using API.Filters;
using Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace API.Tests.Filters
{
    /// <summary>
    /// What turns a bad form away before the controller sees it.
    /// </summary>
    public sealed class ValidateModelFilterTests
    {
        private static ActionExecutingContext Context(params (string Field, string Error)[] errors)
        {
            ActionContext action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            foreach ((string field, string error) in errors)
                action.ModelState.AddModelError(field, error);

            return new ActionExecutingContext(action, [], new Dictionary<string, object?>(), new object());
        }

        private static string[] ErrorsOf(ActionExecutingContext context)
        {
            ApiResponse<object> response = context.Result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ApiResponse<object>>();
            response.IsSucceed.ShouldBeFalse();
            response.Data.ShouldBeNull();

            return response.Messages.ShouldHaveSingleItem().Value;
        }

        [Fact]
        public void A_good_form_goes_through()
        {
            ActionExecutingContext context = Context();
            context.ModelState.SetModelValue("Name", "x", "x");
            context.ModelState.MarkFieldValid("Name");

            new ValidateModelFilter().OnActionExecuting(context);

            context.Result.ShouldBeNull();
        }

        // One that nobody has looked at is as good as bad, and there is nothing to say about it.
        [Fact]
        public void A_form_that_was_not_looked_at_is_turned_away()
        {
            ActionExecutingContext context = Context();
            context.ModelState.SetModelValue("Name", "x", "x");

            new ValidateModelFilter().OnActionExecuting(context);

            ErrorsOf(context).ShouldBeEmpty();
        }

        // Every one of them, whichever field it is of, and the fields are not named.
        [Fact]
        public void A_bad_form_is_answered_with_everything_that_is_wrong_with_it()
        {
            ActionExecutingContext context = Context(("Name", "One"), ("Name", "Two"), ("$.trainerId", "Three"), ("", "Four"));

            new ValidateModelFilter().OnActionExecuting(context);

            ErrorsOf(context).ShouldBe(["One", "Two", "Three", "Four"], ignoreOrder: true);
        }

        // Something that went wrong without a word for it.
        [Fact]
        public void An_error_with_no_text_is_an_empty_one()
        {
            ActionExecutingContext context = Context();
            context.ModelState.TryAddModelException("Name", new InputFormatterException("Not for the client."));
            context.ModelState.TryAddModelException("Other", new InvalidOperationException("Not for the client either."));

            new ValidateModelFilter().OnActionExecuting(context);

            ErrorsOf(context).ShouldBe(["Not for the client.", ""], ignoreOrder: true);
        }

        [Fact]
        public void Afterwards_it_does_nothing()
        {
            ActionContext action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            ActionExecutedContext context = new ActionExecutedContext(action, [], new object()) { Result = new OkResult() };

            new ValidateModelFilter().OnActionExecuted(context);

            context.Result.ShouldBeOfType<OkResult>();
        }
    }
}
