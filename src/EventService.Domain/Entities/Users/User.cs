using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Events;
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
    public List<UserUserGroup> UserUserGroups { get; private set; } = new();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public List<EventAttendee> EventAttendees { get; private set; } = new();

    private User() { }

    private User(string name, string email, string phoneNumber, Business business)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        Business = business;
        BusinessId = business.Id;
    }

    // ✅ Factory method for creating a new user
    public static User Create(string name, string email, string phoneNumber, Business business)
    {
        return new User(name, email, phoneNumber, business);
    }

    // ✅ Factory method for updating an existing user
    public User Update(string name, string email, string phoneNumber)
    {
        return new User(name, email, phoneNumber, Business)
        {
            Id = this.Id,
            CreatedAt = this.CreatedAt // Preserve creation date
        };
    }
}


