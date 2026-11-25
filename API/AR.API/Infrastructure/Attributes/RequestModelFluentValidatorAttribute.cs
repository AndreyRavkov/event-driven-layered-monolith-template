using System.Collections;
using AR.ApiServices;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AR.API.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequestModelFluentValidatorAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var actionArgument in context.ActionArguments)
        {
            if (actionArgument.Value != null)
            {
                if (actionArgument.Value is IBaseValidationModel model)
                {
                    var modelType = actionArgument.Value.GetType();
                    var genericType = typeof(IValidator<>).MakeGenericType(modelType);
                    var validator = context.HttpContext.RequestServices.GetService(genericType);

                    if (validator != null)
                    {
                        model.Validate(validator, model);
                    }
                }
                else if (actionArgument.Value.GetType().GetInterfaces()
                                                       .Any(x =>
                                                            x.IsGenericType
                                                            && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                                            && typeof(IBaseValidationModel).IsAssignableFrom(x.GetGenericArguments()[0])))
                {
                    var value = ((IEnumerable<IBaseValidationModel>)actionArgument.Value).FirstOrDefault();

                    if (value != null)
                    {
                        var modelType = value.GetType();
                        var genericType = typeof(IValidator<>).MakeGenericType(modelType);
                        var validator = context.HttpContext.RequestServices.GetService(genericType);

                        if (validator != null)
                        {
                            foreach (IBaseValidationModel item in (IEnumerable)actionArgument.Value)
                            {
                                item.Validate(validator, item);
                            }
                        }
                    }
                }
            }
        }

        base.OnActionExecuting(context);
    }
}