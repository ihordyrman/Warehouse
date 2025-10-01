using Microsoft.Extensions.DependencyInjection;
using Warehouse.Core.Abstractions.EventBus;

namespace Warehouse.Core.Application.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
        => services.AddSingleton<IEventBus, InMemoryEventBus>();

    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        return services;
    }
}
