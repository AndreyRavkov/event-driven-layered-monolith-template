using AR.NetExt.Utils.Collections;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AR.API.Infrastructure.Attributes.Binders;

internal abstract class CommaSeparatedBaseModelBinder<T> : IModelBinder
{
    protected abstract T Parse(string value);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelName = bindingContext.ModelName;

        // Try to fetch the value of the argument by name
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        // Check if the argument value is null or empty
        if (string.IsNullOrEmpty(value))
        {
            // Set null so nullable query params are handled
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var values = value.Split(',');

        bindingContext.Model = values.Select(Parse).ToListExt();
        bindingContext.Result = ModelBindingResult.Success(values.Select(Parse).ToListExt());
        return Task.CompletedTask;
    }
}