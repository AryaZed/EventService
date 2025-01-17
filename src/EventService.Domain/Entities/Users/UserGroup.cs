using EventService.Domain.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Users;

public class UserGroup
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; }
    public List<User> Users { get; private set; } = new();

    private UserGroup(string name, Business business)
    {
        Name = name;
        Business = business;
        BusinessId = business.Id;
    }

    public static UserGroup Create(string name, Business business)
    {
        return new UserGroup(name, business);
    }
}

