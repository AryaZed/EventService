using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Events;
using EventService.Application.Interfaces.Services.Integrations;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Application.Interfaces.Services.RateLimiting;
using EventService.Application.Services.Caching;
using EventService.Application.Services.Events;
using EventService.Application.Services.Integrations;
using EventService.Application.Services.Notifications;
using EventService.Application.Services.RateLimiting;
using EventService.Infrastructure.Configurations;
using EventService.Infrastructure.Consumers.Events;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Persistence.Repositories;
using EventService.Workers.Services.Events;
using EventService.Workers.Services.Notifications;
using EventService.Workers.Services.Webhooks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using StackExchange.Redis;

namespace EventService.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
        });

        services.AddSingleton(ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]));
        services.AddSingleton<ICacheService, RedisCacheService>();

        if (configuration.GetValue<bool>("UseRedisRateLimiting"))
        {
            services.AddSingleton<IRateLimitStore, RedisRateLimitStore>();
        }
        else
        {
            services.AddSingleton<IRateLimitStore, MemoryRateLimitStore>();
        }

        services.AddHttpClient();

        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventProcessor, EventProcessor>();
        services.AddScoped<INotificationService, SmsNotificationService>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IEventAnalyticsRepository, EventAnalyticsRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IWebhookService, WebhookService>();

        var rabbitMQOptions = new RabbitMQOptions();
        configuration.GetSection("RabbitMQ").Bind(rabbitMQOptions);
        services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));

        services.AddMassTransit(config =>
        {
            config.SetKebabCaseEndpointNameFormatter();
            config.AddConsumer<EventConsumer>();

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMQOptions.Host, h =>
                {
                    h.Username(rabbitMQOptions.Username);
                    h.Password(rabbitMQOptions.Password);
                });

                cfg.ReceiveEndpoint("event-processing-queue", e =>
                {
                    e.ConfigureConsumer<EventConsumer>(context);
                    e.PrefetchCount = 100; // ✅ Process 100 messages at a time
                    e.UseConcurrencyLimit(10); // ✅ Allows up to 10 parallel executions
                    e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
                });
            });
        });

        services.AddHostedService<EventProcessingService>();
        services.AddHostedService<EventPrefetchService>();
        services.AddHostedService<FailedNotificationProcessor>();
        services.AddHostedService<WebhookRetryService>();
        services.AddHostedService<FailedWebhookProcessor>();
        services.AddHostedService<WebhookFailureMonitorService>();

        return services;
    }
}