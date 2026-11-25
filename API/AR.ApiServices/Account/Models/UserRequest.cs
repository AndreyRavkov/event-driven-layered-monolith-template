namespace AR.ApiServices.Account.Models;

public class UserRequest : BaseValidationModel<UserRequest>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}