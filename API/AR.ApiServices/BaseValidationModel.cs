using FluentValidation;

namespace AR.ApiServices;

public interface IBaseValidationModel
{
    void Validate(object validator, IBaseValidationModel modelObj);
}

public abstract class BaseValidationModel<T> : IBaseValidationModel
{
    public void Validate(object validator, IBaseValidationModel modelObj)
    {
        var instance = (IValidator<T>)validator;
        ArgumentNullException.ThrowIfNull(instance);
        var result = instance.Validate((T)modelObj);

        if (!result.IsValid && result.Errors.Any())
        {
            throw new ValidationException($"Error(s) occured due validate '{typeof(T).Name}' request model", result.Errors);
        }
    }
}