using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Businesses;

public class Business
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public string ContactEmail { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public List<User> Users { get; private set; } = new();

    private Business() { }

    private Business(string name, string contactEmail, string phoneNumber)
    {
        Name = name;
        ContactEmail = contactEmail;
        PhoneNumber = phoneNumber;
    }

    private Business(Guid id)
    {
        Id = id;
    }

    public static Business Create(string name, string contactEmail, string phoneNumber)
    {
        return new Business(name, contactEmail, phoneNumber);
    }

    public static Business CreateExisting(Guid id)
    {
        return new Business(id);
    }
}

