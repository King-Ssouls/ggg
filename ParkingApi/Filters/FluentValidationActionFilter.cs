using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ParkingApi.Filters;

public sealed class FluentValidationActionFilter : IAsyncActionFilter, IOrderedFilter
{
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    public FluentValidationActionFilter(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
    {
        _apiBehaviorOptions = apiBehaviorOptions.Value;
    }

    // Фильтр запускается раньше автоматической проверки ModelState у ApiController
    public int Order => -3000;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Для каждого аргумента ищется соответствующий валидатор, зарегистрированный в Program.cs
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        // Возвращает общий ответ 400, если FluentValidation добавил ошибки в ModelState
        if (!context.ModelState.IsValid)
        {
            context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }
}
