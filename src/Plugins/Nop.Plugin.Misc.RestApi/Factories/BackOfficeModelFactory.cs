using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.RestApi.Factories;

/// <summary>
/// Prepares the models of the back-office API (raw admin values: base prices in the primary store currency)
/// </summary>
public class BackOfficeModelFactory
{
    #region Fields

    protected readonly ApiModelFactory _apiModelFactory;
    protected readonly ICategoryService _categoryService;
    protected readonly ICustomerService _customerService;
    protected readonly IOrderService _orderService;
    protected readonly IShipmentService _shipmentService;

    #endregion

    #region Ctor

    public BackOfficeModelFactory(ApiModelFactory apiModelFactory,
        ICategoryService categoryService,
        ICustomerService customerService,
        IOrderService orderService,
        IShipmentService shipmentService)
    {
        _apiModelFactory = apiModelFactory;
        _categoryService = categoryService;
        _customerService = customerService;
        _orderService = orderService;
        _shipmentService = shipmentService;
    }

    #endregion

    #region Methods

    public virtual async Task<AdminProductDto> PrepareProductAsync(Product product)
    {
        return new AdminProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Sku = product.Sku,
            Price = product.Price,
            OldPrice = product.OldPrice,
            StockQuantity = product.StockQuantity,
            Published = product.Published,
            VendorId = product.VendorId,
            CategoryIds = (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id, true)).Select(pc => pc.CategoryId).ToList(),
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc
        };
    }

    public virtual AdminCategoryDto PrepareCategory(Category category)
    {
        return new AdminCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            Published = category.Published,
            CreatedOnUtc = category.CreatedOnUtc,
            UpdatedOnUtc = category.UpdatedOnUtc
        };
    }

    public virtual async Task<AdminCustomerDto> PrepareCustomerAsync(Customer customer)
    {
        return new AdminCustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            Username = customer.Username,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Company = customer.Company,
            Phone = customer.Phone,
            Active = customer.Active,
            Roles = (await _customerService.GetCustomerRolesAsync(customer)).Select(r => r.SystemName ?? r.Name).ToList(),
            CreatedOnUtc = customer.CreatedOnUtc,
            LastActivityDateUtc = customer.LastActivityDateUtc
        };
    }

    public virtual async Task<AdminOrderDto> PrepareOrderAsync(Order order)
    {
        var detail = await _apiModelFactory.PrepareOrderDetailAsync(order, await _orderService.GetOrderItemsAsync(order.Id));
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

        return new AdminOrderDto
        {
            Id = detail.Id,
            CustomOrderNumber = detail.CustomOrderNumber,
            CreatedOnUtc = detail.CreatedOnUtc,
            OrderStatus = detail.OrderStatus,
            PaymentStatus = detail.PaymentStatus,
            ShippingStatus = detail.ShippingStatus,
            OrderTotal = detail.OrderTotal,
            OrderTotalFormatted = detail.OrderTotalFormatted,
            CurrencyCode = detail.CurrencyCode,
            PaymentMethod = detail.PaymentMethod,
            ShippingMethod = detail.ShippingMethod,
            OrderSubtotal = detail.OrderSubtotal,
            OrderShipping = detail.OrderShipping,
            OrderTax = detail.OrderTax,
            BillingAddress = detail.BillingAddress,
            ShippingAddress = detail.ShippingAddress,
            Items = detail.Items,
            CustomerId = order.CustomerId,
            CustomerEmail = customer?.Email,
            StoreId = order.StoreId
        };
    }

    public virtual async Task<AdminShipmentDto> PrepareShipmentAsync(Shipment shipment)
    {
        return new AdminShipmentDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            TrackingNumber = shipment.TrackingNumber,
            TotalWeight = shipment.TotalWeight,
            ShippedDateUtc = shipment.ShippedDateUtc,
            DeliveryDateUtc = shipment.DeliveryDateUtc,
            CreatedOnUtc = shipment.CreatedOnUtc,
            Items = (await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id))
                .Select(i => new AdminShipmentItemDto(i.OrderItemId, i.Quantity)).ToList()
        };
    }

    public virtual AdminReturnRequestDto PrepareReturnRequest(ReturnRequest request)
    {
        return new AdminReturnRequestDto
        {
            Id = request.Id,
            CustomNumber = request.CustomNumber,
            CustomerId = request.CustomerId,
            OrderItemId = request.OrderItemId,
            Quantity = request.Quantity,
            ReasonForReturn = request.ReasonForReturn,
            RequestedAction = request.RequestedAction,
            CustomerComments = request.CustomerComments,
            StaffNotes = request.StaffNotes,
            Status = request.ReturnRequestStatus.ToString(),
            CreatedOnUtc = request.CreatedOnUtc
        };
    }

    #endregion
}
