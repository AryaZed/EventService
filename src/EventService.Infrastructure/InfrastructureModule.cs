using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Application.Services.Events;
using EventService.Application.Services.Notifications;
using EventService.Infrastructure.Configurations;
using EventService.Infrastructure.Consumers.Events;
using EventService.Infrastructure.Persistence;
using EventService.Infrastructure.Persistence.Repositories;
using EventService.Workers.Services.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventProcessor, EventProcessor>();
        services.AddScoped<INotificationService, SmsNotificationService>();

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
                    e.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2))); // ✅ Retry failed messages
                });
            });
        });

        services.AddHostedService<EventProcessingService>();

        return services;
    }
}