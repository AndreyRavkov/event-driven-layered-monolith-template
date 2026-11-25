namespace AR.Infrastructure.Jwt.DTO;

public class JwtTokenDtoModel
{
    public required int UserId { get; init; }
    public required string UserName { get; init; }
}