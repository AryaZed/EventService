using EventService.Domain.Entities.Users;

namespace EventService.WebApi.Features.Users;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; }

    public static UserResponse FromEntity(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            BusinessId = user.BusinessId,
            BusinessName = user.Business?.Name ?? ""
        };
    }
}