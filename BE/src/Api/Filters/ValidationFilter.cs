using Backend.Shared.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator (if one is registered) for every
/// non-primitive action argument before the action executes, short-circuiting
/// with a 400 <see cref="ApiResponse"/> on failure. Controllers stay free of
/// manual "if (!ModelState.IsValid) return BadRequest(...)" boilerplate.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                context.Result = new BadRequestObjectResult(ApiResponse.Fail("Validation failed.", errors));
                return;
            }
        }

        await next();
    }
}
