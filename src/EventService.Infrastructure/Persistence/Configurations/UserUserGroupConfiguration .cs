using EventService.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Configurations;

public class UserUserGroupConfiguration : IEntityTypeConfiguration<UserUserGroup>
{
    public void Configure(EntityTypeBuilder<UserUserGroup> builder)
    {
        builder.HasKey(ug => new { ug.UserId, ug.UserGroupId }); // ✅ Composite Key for Many-to-Many

        builder.HasOne(ug => ug.User)
            .WithMany(u => u.UserUserGroups) // ✅ Ensure Navigation Property Exists in `User.cs`
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents cascading delete conflicts

        builder.HasOne(ug => ug.UserGroup)
            .WithMany(g => g.UserUserGroups) // ✅ Ensure Navigation Property Exists in `UserGroup.cs`
            .HasForeignKey(ug => ug.UserGroupId)
            .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents cascading delete conflicts
    }
}
