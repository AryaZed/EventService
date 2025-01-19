using EventService.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Status).IsRequired();
    }
}
