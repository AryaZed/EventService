using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Workers.Services.Events
{
    public class EventPrefetchService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventPrefetchService> _logger;
        private static readonly TimeSpan PrefetchInterval = TimeSpan.FromMinutes(5);

        public EventPrefetchService(IServiceScopeFactory serviceScopeFactory, ILogger<EventPrefetchService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var businessRepository = scope.ServiceProvider.GetRequiredService<IBusinessRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var businesses = await businessRepository.GetAllAsync();
                foreach (var business in businesses)
                {
                    var events = await eventRepository.GetEventsByBusinessIdAsync(business.Id);
                    var upcomingEvents = events.Where(e => e.ScheduledAt >= DateTime.UtcNow)
                                               .OrderBy(e => e.ScheduledAt)
                                               .Take(5)
                                               .ToList();

                    await cacheService.SetAsync($"events:upcoming:{business.Id}", upcomingEvents, TimeSpan.FromMinutes(5));
                }

                _logger.LogInformation("Event prefetching completed at {Time}", DateTime.UtcNow);
                await Task.Delay(PrefetchInterval, stoppingToken);
            }
        }
    }
}
