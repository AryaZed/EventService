namespace EventService.WebApi.Features.Businesses;

public class BusinessRequest
{
    public required string Name { get; set; }
    public required string ContactEmail { get; set; }
    public required string PhoneNumber { get; set; }
}