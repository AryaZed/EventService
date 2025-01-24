using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
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
            _logger.LogInformation("🚀 Event Prefetch Service Started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("🔍 Checking for upcoming events...");

                    using var scope = _serviceScopeFactory.CreateScope();
                    var businessRepository = scope.ServiceProvider.GetRequiredService<IBusinessRepository>();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                    try
                    {
                        var businesses = await businessRepository.GetAllAsync(stoppingToken); // ✅ Pass CancellationToken

                        foreach (var business in businesses)
                        {
                            if (stoppingToken.IsCancellationRequested) return; // ✅ Stop immediately if requested

                            var events = await eventRepository.GetEventsByBusinessIdAsync(business.Id, stoppingToken);
                            var upcomingEvents = events.Where(e => e.ScheduledAt >= DateTime.UtcNow)
                                                       .OrderBy(e => e.ScheduledAt)
                                                       .Take(5)
                                                       .ToList();

                            await cacheService.SetAsync($"events:upcoming:{business.Id}", upcomingEvents, TimeSpan.FromMinutes(5));
                        }

                        _logger.LogInformation("✅ Event prefetching completed at {Time}", DateTime.UtcNow);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("⚠️ Event Prefetch Service is stopping due to cancellation.");
                        break; // ✅ Gracefully exit loop
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Unexpected error while prefetching events.");
                    }

                    await Task.Delay(PrefetchInterval, stoppingToken); // ✅ Delay with proper cancellation support
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Event Prefetch Service was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Critical error in Event Prefetch Service.");
            }
            finally
            {
                _logger.LogInformation("🔴 Event Prefetch Service Stopped.");
            }
        }
    }
}
