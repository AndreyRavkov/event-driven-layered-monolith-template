using FluentValidation;
using JetBrains.Annotations;

namespace AR.ApiServices.Account.Models.Validators;

[UsedImplicitly]
public class UserRequestValidator : AbstractValidator<UserRequest>
{
    public UserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}