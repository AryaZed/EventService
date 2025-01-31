using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Events
{
    public class EventAttendee
    {
        public Guid EventId { get; private set; }
        public Event Event { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; }
        public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

        private EventAttendee() { }

        public EventAttendee(Event eventEntity, User user)
        {
            EventId = eventEntity.Id;
            Event = eventEntity;
            UserId = user.Id;
            User = user;
        }
    }
}
