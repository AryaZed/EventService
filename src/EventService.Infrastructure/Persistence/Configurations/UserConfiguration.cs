using EventService.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(255);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(50);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasOne(u => u.Business)
            .WithMany()
            .HasForeignKey(u => u.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
