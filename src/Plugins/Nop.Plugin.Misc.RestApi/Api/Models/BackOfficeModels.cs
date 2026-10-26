namespace Nop.Plugin.Misc.RestApi.Api.Models;

#region Catalog

public record AdminProductDto
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string ShortDescription { get; init; }

    public string FullDescription { get; init; }

    public string Sku { get; init; }

    /// <summary>
    /// Base price in the primary store currency, as entered in the admin area
    /// </summary>
    public decimal Price { get; init; }

    public decimal OldPrice { get; init; }

    public int StockQuantity { get; init; }

    public bool Published { get; init; }

    public int VendorId { get; init; }

    public IList<int> CategoryIds { get; init; } = [];

    public DateTime CreatedOnUtc { get; init; }

    public DateTime UpdatedOnUtc { get; init; }
}

/// <summary>
/// Product creation and update; null properties are left unchanged on update
/// </summary>
public record AdminProductRequest
{
    public string Name { get; init; }

    public string ShortDescription { get; init; }

    public string FullDescription { get; init; }

    public string Sku { get; init; }

    public decimal? Price { get; init; }

    public decimal? OldPrice { get; init; }

    public int? StockQuantity { get; init; }

    public bool? Published { get; init; }

    public IList<int> CategoryIds { get; init; }
}

public record AdminCategoryDto
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public int ParentCategoryId { get; init; }

    public int DisplayOrder { get; init; }

    public bool Published { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime UpdatedOnUtc { get; init; }
}

/// <summary>
/// Category creation and update; null properties are left unchanged on update
/// </summary>
public record AdminCategoryRequest
{
    public string Name { get; init; }

    public string Description { get; init; }

    public int? ParentCategoryId { get; init; }

    public int? DisplayOrder { get; init; }

    public bool? Published { get; init; }
}

#endregion

#region Customers

public record AdminCustomerDto
{
    public int Id { get; init; }

    public string Email { get; init; }

    public string Username { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string Company { get; init; }

    public string Phone { get; init; }

    public bool Active { get; init; }

    public IList<string> Roles { get; init; } = [];

    public DateTime CreatedOnUtc { get; init; }

    public DateTime LastActivityDateUtc { get; init; }
}

/// <summary>
/// Customer update; null properties are left unchanged
/// </summary>
public record AdminCustomerRequest
{
    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string Company { get; init; }

    public string Phone { get; init; }

    public bool? Active { get; init; }
}

#endregion

#region Orders

public record AdminOrderDto : OrderDetailDto
{
    public int CustomerId { get; init; }

    public string CustomerEmail { get; init; }

    public int StoreId { get; init; }
}

public record AdminShipmentItemDto(int OrderItemId, int Quantity);

public record AdminShipmentDto
{
    public int Id { get; init; }

    public int OrderId { get; init; }

    public string TrackingNumber { get; init; }

    public decimal? TotalWeight { get; init; }

    public DateTime? ShippedDateUtc { get; init; }

    public DateTime? DeliveryDateUtc { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public IList<AdminShipmentItemDto> Items { get; init; } = [];
}

/// <summary>
/// Creates a shipment; without items, every not yet shipped item of the order is added
/// </summary>
public record AdminCreateShipmentRequest
{
    public string TrackingNumber { get; init; }

    public string AdminComment { get; init; }

    public IList<AdminShipmentItemDto> Items { get; init; }

    public bool Ship { get; init; }

    public bool NotifyCustomer { get; init; } = true;
}

public record AdminReturnRequestDto
{
    public int Id { get; init; }

    public string CustomNumber { get; init; }

    public int CustomerId { get; init; }

    public int OrderItemId { get; init; }

    public int Quantity { get; init; }

    public string ReasonForReturn { get; init; }

    public string RequestedAction { get; init; }

    public string CustomerComments { get; init; }

    public string StaffNotes { get; init; }

    public string Status { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}

/// <summary>
/// Return request update; null properties are left unchanged
/// </summary>
public record AdminUpdateReturnRequest
{
    public Nop.Core.Domain.Orders.ReturnRequestStatus? Status { get; init; }

    public string StaffNotes { get; init; }
}

#endregion
