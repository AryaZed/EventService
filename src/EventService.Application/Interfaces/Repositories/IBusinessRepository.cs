﻿using EventService.Domain.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories;

public interface IBusinessRepository
{
    Task<IEnumerable<Business>> GetAllAsync(CancellationToken stoppingToken);
    Task<Business?> GetByIdAsync(Guid id);
    Task AddAsync(Business business);
    Task UpdateAsync(Business business);
    Task DeleteAsync(Guid id);
    Task<List<Business>> GetExpiredSubscriptionsAsync();
    Task<Business?> GetBusinessByTenantAsync(Guid tenantId);
}