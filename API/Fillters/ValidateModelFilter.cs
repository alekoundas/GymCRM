using Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ValidateModelFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                List<string> validationErrors = new List<string>();
                foreach (var error in context.ModelState)
                {
                    foreach (var errorDetail in error.Value.Errors)
                    {
                        // Clean up field name (e.g., remove "$.trainerId" to "trainerId")
                        var fieldName = error.Key.StartsWith("$.") ? error.Key.Substring(2) : error.Key;
                        validationErrors.Add(errorDetail.ErrorMessage);
                    }
                }

                var response = new ApiResponse<object>().SetErrorResponse(validationErrors.ToArray());
                context.Result = new BadRequestObjectResult(response);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}