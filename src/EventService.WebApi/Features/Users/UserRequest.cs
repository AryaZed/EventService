namespace EventService.WebApi.Features.Users;
public class UserRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required Guid BusinessId { get; set; }
}