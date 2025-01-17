using EventService.Domain.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Users;

public class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; }
    public List<UserGroup> Groups { get; private set; } = new();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private User(string name, string email, string phoneNumber, Business business, List<UserGroup>? groups = null)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        Business = business;
        BusinessId = business.Id;
        Groups = groups ?? new List<UserGroup>();
    }

    // ✅ Factory method for creating a new user
    public static User Create(string name, string email, string phoneNumber, Business business, List<UserGroup>? groups = null)
    {
        return new User(name, email, phoneNumber, business, groups);
    }

    // ✅ Factory method for updating an existing user
    public User Update(string name, string email, string phoneNumber, List<UserGroup>? groups = null)
    {
        return new User(name, email, phoneNumber, Business, groups ?? Groups)
        {
            Id = this.Id,
            CreatedAt = this.CreatedAt // Preserve creation date
        };
    }
}


