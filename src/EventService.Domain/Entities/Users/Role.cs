using EventService.Domain.Entities.Businesses;

namespace EventService.Domain.Entities.Users;

public class Role
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }

    public Guid BusinessId { get; private set; } // ✅ Multi-Tenancy
    public Business Business { get; private set; }

    public List<User> Users { get; private set; } = new();

    private Role() { }

    public Role(string name, Business business)
    {
        Name = name;
        Business = business ?? throw new ArgumentNullException(nameof(business));
        BusinessId = business.Id;
    }
}
