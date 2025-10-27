using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Backend.Endpoints.Models;
using Warehouse.Backend.Endpoints.Validation;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Backend.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/orders");
        group.WithTags("orders");
        group.RequireRateLimiting("ApiPolicy");

        group.MapPost(
                "/",
                async Task<Results<Created<OrderResponse>, BadRequest<string>>> (IOrderManager orderManager, CreateOrderApiRequest request)
                    =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    CreateOrderRequest serviceRequest = request.ToServiceRequest();
                    Result<Order> result = await orderManager.CreateOrderAsync(serviceRequest);

                    if (!result.IsSuccess)
                    {
                        return TypedResults.BadRequest(result.Error.Message);
                    }

                    Result<Order> placeResult = await orderManager.ExecuteOrderAsync(result.Value.Id);
                    if (!placeResult.IsSuccess)
                    {
                        return TypedResults.BadRequest($"Order created but failed to place: {placeResult.Error.Message}");
                    }

                    result = placeResult;

                    var response = OrderResponse.FromDomain(result.Value);
                    return TypedResults.Created($"/orders/{response.Id}", response);
                })
            .WithName("CreateOrder")
            .WithSummary("Create a new order")
            .Produces<OrderResponse>(201)
            .Produces<string>(400);

        group.MapGet(
                "/{id:long}",
                async Task<Results<Ok<OrderResponse>, NotFound>> (IOrderManager orderManager, long id) =>
                {
                    Order? order = await orderManager.GetOrderAsync(id);

                    return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderResponse.FromDomain(order));
                })
            .WithName("GetOrder")
            .WithSummary("Get an order by ID")
            .Produces<OrderResponse>()
            .Produces(404);

        group.MapPut(
                "/{id:long}",
                async Task<Results<Ok<OrderResponse>, NotFound, BadRequest<string>>> (
                    IOrderManager orderManager,
                    long id,
                    UpdateOrderApiRequest request) =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    UpdateOrderRequest serviceRequest = request.ToServiceRequest();
                    Result<Order> result = await orderManager.UpdateOrderAsync(id, serviceRequest);

                    if (result.IsSuccess)
                    {
                        return TypedResults.Ok(OrderResponse.FromDomain(result.Value));
                    }

                    if (result.Error.Message.Contains("not found"))
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.BadRequest(result.Error.Message);
                })
            .WithName("UpdateOrder")
            .WithSummary("Update an existing order")
            .Produces<OrderResponse>()
            .Produces(404)
            .Produces<string>(400);

        group.MapPost(
                "/{id:long}/place",
                async Task<Results<Ok<OrderResponse>, NotFound, BadRequest<string>>> (IOrderManager orderManager, long id) =>
                {
                    Result<Order> result = await orderManager.ExecuteOrderAsync(id);

                    if (result.IsSuccess)
                    {
                        return TypedResults.Ok(OrderResponse.FromDomain(result.Value));
                    }

                    if (result.Error.Message.Contains("not found"))
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.BadRequest(result.Error.Message);
                })
            .WithName("ExecuteOrder")
            .WithSummary("Execute order on the exchange")
            .Produces<OrderResponse>()
            .Produces(404)
            .Produces<string>(400);

        group.MapPost(
                "/{id:long}/cancel",
                async Task<Results<Ok<OrderResponse>, NotFound, BadRequest<string>>> (
                    IOrderManager orderManager,
                    long id,
                    [FromBody] CancelOrderRequest? request) =>
                {
                    Result<Order> result = await orderManager.CancelOrderAsync(id, request?.Reason);

                    if (result.IsSuccess)
                    {
                        return TypedResults.Ok(OrderResponse.FromDomain(result.Value));
                    }

                    if (result.Error.Message.Contains("not found"))
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.BadRequest(result.Error.Message);
                })
            .WithName("CancelOrder")
            .WithSummary("Cancel an order")
            .Produces<OrderResponse>()
            .Produces(404)
            .Produces<string>(400);

        group.MapGet(
                "/worker/{workerId:int}",
                async Task<Ok<List<OrderResponse>>> (IOrderManager orderManager, int workerId, [FromQuery] OrderStatus? status) =>
                {
                    List<Order> orders = await orderManager.GetOrdersAsync(workerId, status);
                    var response = orders.Select(OrderResponse.FromDomain).ToList();
                    return TypedResults.Ok(response);
                })
            .WithName("GetOrdersByWorker")
            .WithSummary("Get all orders for a specific worker")
            .Produces<List<OrderResponse>>();

        group.MapPost(
                "/history",
                async Task<Ok<List<OrderResponse>>> (IOrderManager orderManager, [FromBody] OrderHistoryFilterRequest request) =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    OrderHistoryFilter filter = request.ToServiceFilter();
                    List<Order> orders = await orderManager.GetOrderHistoryAsync(request.Skip, request.Take, filter);
                    var response = orders.Select(OrderResponse.FromDomain).ToList();
                    return TypedResults.Ok(response);
                })
            .WithName("GetOrderHistory")
            .WithSummary("Get order history with filtering and pagination")
            .Produces<List<OrderResponse>>();

        return group;
    }
}
