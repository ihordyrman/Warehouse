using Warehouse.Backend.Core.Abstractions.EventBus;

namespace Warehouse.Backend.Core.Application.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }

    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        return services;
    }
}
