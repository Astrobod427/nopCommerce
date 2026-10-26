using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Back office: orders, shipments and return requests, with the permissions of the admin area (Sales).
/// Status changes go through the nopCommerce order processing service (notes, e-mails, events, stock).
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/admin")]
[ApiAuthorize(ApiTokenKind.BackOffice)]
public class BackOfficeOrdersController : BaseApiController
{
    #region Fields

    protected readonly BackOfficeModelFactory _backOfficeModelFactory;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly IProductService _productService;
    protected readonly IReturnRequestService _returnRequestService;
    protected readonly IShipmentService _shipmentService;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public BackOfficeOrdersController(BackOfficeModelFactory backOfficeModelFactory,
        ICustomerActivityService customerActivityService,
        IEventPublisher eventPublisher,
        ILocalizationService localizationService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IProductService productService,
        IReturnRequestService returnRequestService,
        IShipmentService shipmentService,
        RestApiSettings settings)
    {
        _backOfficeModelFactory = backOfficeModelFactory;
        _customerActivityService = customerActivityService;
        _eventPublisher = eventPublisher;
        _localizationService = localizationService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _productService = productService;
        _returnRequestService = returnRequestService;
        _shipmentService = shipmentService;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Order> GetOrderAsync(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order is { Deleted: false } ? order : null;
    }

    protected virtual async Task LogEditOrderAsync(Order order)
    {
        await _customerActivityService.InsertActivityAsync("EditOrder",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditOrder"), order.CustomOrderNumber), order);
    }

    protected virtual async Task AddOrderNoteAsync(Order order, string note)
    {
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = note,
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
    }

    #endregion

    #region Orders

    /// <summary>
    /// Searches orders (statuses by name, e.g. orderStatus=Pending, paymentStatus=Paid, shippingStatus=NotYetShipped)
    /// </summary>
    [HttpGet("orders")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.ORDERS_VIEW)]
    public virtual async Task<IActionResult> Orders([FromQuery] int? customerId, [FromQuery] OrderStatus? orderStatus,
        [FromQuery] PaymentStatus? paymentStatus, [FromQuery] ShippingStatus? shippingStatus,
        [FromQuery] DateTime? createdFromUtc, [FromQuery] DateTime? createdToUtc,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0)
    {
        var orders = await _orderService.SearchOrdersAsync(customerId: customerId ?? 0,
            createdFromUtc: createdFromUtc, createdToUtc: createdToUtc,
            osIds: orderStatus.HasValue ? [(int)orderStatus.Value] : null,
            psIds: paymentStatus.HasValue ? [(int)paymentStatus.Value] : null,
            ssIds: shippingStatus.HasValue ? [(int)shippingStatus.Value] : null,
            pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings));

        var items = new List<AdminOrderDto>();
        foreach (var order in orders)
            items.Add(await _backOfficeModelFactory.PrepareOrderAsync(order));

        return ApiOk(ToPaged(orders, items));
    }

    [HttpGet("orders/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.ORDERS_VIEW)]
    public virtual async Task<IActionResult> Order(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null)
            return ApiNotFound("Order");

        return ApiOk(await _backOfficeModelFactory.PrepareOrderAsync(order));
    }

    /// <summary>
    /// Marks an order as paid (offline payments: bank transfer, cash...)
    /// </summary>
    [HttpPost("orders/{id:int}/mark-paid")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.ORDERS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> MarkPaid(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null)
            return ApiNotFound("Order");

        if (!_orderProcessingService.CanMarkOrderAsPaid(order))
            return ApiError(StatusCodes.Status409Conflict, "invalid_state", "This order cannot be marked as paid.");

        await _orderProcessingService.MarkOrderAsPaidAsync(order);
        await LogEditOrderAsync(order);

        return ApiOk(await _backOfficeModelFactory.PrepareOrderAsync(order));
    }

    /// <summary>
    /// Cancels an order (stock is restored by nopCommerce)
    /// </summary>
    [HttpPost("orders/{id:int}/cancel")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.ORDERS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Cancel(int id, [FromQuery] bool notifyCustomer = true)
    {
        var order = await GetOrderAsync(id);
        if (order == null)
            return ApiNotFound("Order");

        if (!_orderProcessingService.CanCancelOrder(order))
            return ApiError(StatusCodes.Status409Conflict, "invalid_state", "This order cannot be cancelled.");

        await _orderProcessingService.CancelOrderAsync(order, notifyCustomer);
        await LogEditOrderAsync(order);

        return ApiOk(await _backOfficeModelFactory.PrepareOrderAsync(order));
    }

    #endregion

    #region Shipments

    [HttpGet("orders/{id:int}/shipments")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.SHIPMENTS_VIEW)]
    public virtual async Task<IActionResult> Shipments(int id)
    {
        var order = await GetOrderAsync(id);
        if (order == null)
            return ApiNotFound("Order");

        var items = new List<AdminShipmentDto>();
        foreach (var shipment in await _shipmentService.GetShipmentsByOrderIdAsync(order.Id))
            items.Add(await _backOfficeModelFactory.PrepareShipmentAsync(shipment));

        return ApiOk(items);
    }

    /// <summary>
    /// Creates a shipment (same rules as the admin area); optionally marks it as shipped right away
    /// </summary>
    [HttpPost("orders/{id:int}/shipments")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.SHIPMENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> CreateShipment(int id, [FromBody] AdminCreateShipmentRequest request)
    {
        var order = await GetOrderAsync(id);
        if (order == null)
            return ApiNotFound("Order");

        request ??= new AdminCreateShipmentRequest();
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, isShipEnabled: true);

        var errors = new List<string>();
        foreach (var requested in request.Items ?? [])
        {
            if (orderItems.All(i => i.Id != requested.OrderItemId))
                errors.Add($"Order item {requested.OrderItemId} does not belong to this order or is not shippable.");
        }

        if (errors.Any())
            return ApiBadRequest("Invalid shipment items.", errors);

        var shipmentItems = new List<ShipmentItem>();
        decimal? totalWeight = null;
        foreach (var orderItem in orderItems)
        {
            var maxQuantity = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQuantity <= 0)
                continue;

            var quantity = request.Items == null
                ? maxQuantity
                : request.Items.Where(i => i.OrderItemId == orderItem.Id).Sum(i => i.Quantity);
            if (quantity <= 0)
                continue;

            quantity = Math.Min(quantity, maxQuantity);

            //multiple warehouses are chosen by the store owner in the admin area: not supported through the API
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
            {
                errors.Add($"Order item {orderItem.Id}: the product uses multiple warehouses, create this shipment in the admin area.");
                continue;
            }

            if (orderItem.ItemWeight.HasValue)
                totalWeight = (totalWeight ?? 0) + orderItem.ItemWeight.Value * quantity;

            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = quantity,
                WarehouseId = product.WarehouseId
            });
        }

        if (errors.Any())
            return ApiBadRequest("Invalid shipment items.", errors);

        if (!shipmentItems.Any())
            return ApiBadRequest(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoProductsSelected"));

        var shipment = new Shipment
        {
            OrderId = order.Id,
            TrackingNumber = request.TrackingNumber,
            TotalWeight = totalWeight,
            AdminComment = request.AdminComment,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _shipmentService.InsertShipmentAsync(shipment);
        foreach (var shipmentItem in shipmentItems)
        {
            shipmentItem.ShipmentId = shipment.Id;
            await _shipmentService.InsertShipmentItemAsync(shipmentItem);
        }

        await AddOrderNoteAsync(order, "A shipment has been added (REST API)");
        await _eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment));
        if (!string.IsNullOrWhiteSpace(shipment.TrackingNumber))
            await _eventPublisher.PublishAsync(new ShipmentTrackingNumberSetEvent(shipment));

        if (request.Ship && !order.PickupInStore)
            await _orderProcessingService.ShipAsync(shipment, request.NotifyCustomer);

        await LogEditOrderAsync(order);

        return ApiCreated(await _backOfficeModelFactory.PrepareShipmentAsync(shipment));
    }

    [HttpPost("shipments/{id:int}/ship")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.SHIPMENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Ship(int id, [FromQuery] bool notifyCustomer = true)
    {
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        var order = shipment == null ? null : await GetOrderAsync(shipment.OrderId);
        if (order == null)
            return ApiNotFound("Shipment");

        if (shipment.ShippedDateUtc.HasValue || order.PickupInStore)
            return ApiError(StatusCodes.Status409Conflict, "invalid_state", "This shipment cannot be marked as shipped.");

        await _orderProcessingService.ShipAsync(shipment, notifyCustomer);
        await LogEditOrderAsync(order);

        return ApiOk(await _backOfficeModelFactory.PrepareShipmentAsync(shipment));
    }

    [HttpPost("shipments/{id:int}/deliver")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.SHIPMENTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Deliver(int id, [FromQuery] bool notifyCustomer = true)
    {
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        var order = shipment == null ? null : await GetOrderAsync(shipment.OrderId);
        if (order == null)
            return ApiNotFound("Shipment");

        if (shipment.DeliveryDateUtc.HasValue || (!shipment.ShippedDateUtc.HasValue && !shipment.ReadyForPickupDateUtc.HasValue))
            return ApiError(StatusCodes.Status409Conflict, "invalid_state", "This shipment cannot be marked as delivered.");

        await _orderProcessingService.DeliverAsync(shipment, notifyCustomer);
        await LogEditOrderAsync(order);

        return ApiOk(await _backOfficeModelFactory.PrepareShipmentAsync(shipment));
    }

    #endregion

    #region Return requests

    [HttpGet("return-requests")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.RETURN_REQUESTS_VIEW)]
    public virtual async Task<IActionResult> ReturnRequests([FromQuery] ReturnRequestStatus? status, [FromQuery] int? customerId,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0)
    {
        var requests = await _returnRequestService.SearchReturnRequestsAsync(customerId: customerId ?? 0, rs: status,
            pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings));

        return ApiOk(ToPaged(requests, requests.Select(_backOfficeModelFactory.PrepareReturnRequest).ToList()));
    }

    [HttpGet("return-requests/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.RETURN_REQUESTS_VIEW)]
    public virtual async Task<IActionResult> ReturnRequest(int id)
    {
        var request = await _returnRequestService.GetReturnRequestByIdAsync(id);
        if (request == null)
            return ApiNotFound("Return request");

        return ApiOk(_backOfficeModelFactory.PrepareReturnRequest(request));
    }

    /// <summary>
    /// Changes the status and/or staff notes of a return request
    /// </summary>
    [HttpPut("return-requests/{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Orders.RETURN_REQUESTS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> UpdateReturnRequest(int id, [FromBody] AdminUpdateReturnRequest request)
    {
        var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);
        if (returnRequest == null)
            return ApiNotFound("Return request");

        if (request == null)
            return ApiBadRequest("A request body is required.");

        if (request.Status.HasValue)
            returnRequest.ReturnRequestStatus = request.Status.Value;
        if (request.StaffNotes != null)
            returnRequest.StaffNotes = request.StaffNotes;
        returnRequest.UpdatedOnUtc = DateTime.UtcNow;
        await _returnRequestService.UpdateReturnRequestAsync(returnRequest);

        await _customerActivityService.InsertActivityAsync("EditReturnRequest",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditReturnRequest"), returnRequest.Id), returnRequest);

        return ApiOk(_backOfficeModelFactory.PrepareReturnRequest(returnRequest));
    }

    #endregion
}
