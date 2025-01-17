using EventService.Domain.Entities.Businesses;

namespace EventService.WebApi.Features.Businesses;

public class BusinessResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ContactEmail { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public static BusinessResponse FromEntity(Business business)
    {
        return new BusinessResponse
        {
            Id = business.Id,
            Name = business.Name,
            ContactEmail = business.ContactEmail,
            PhoneNumber = business.PhoneNumber,
            CreatedAt = business.CreatedAt
        };
    }
}