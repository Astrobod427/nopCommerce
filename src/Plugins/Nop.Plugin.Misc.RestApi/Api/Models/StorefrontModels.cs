namespace Nop.Plugin.Misc.RestApi.Api.Models;

#region Authentication

public record GuestTokenRequest;

public record LoginRequest(string UsernameOrEmail, string Password);

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string Username = null);

public record ClientTokenRequest(string ClientId, string ClientSecret);

public record TokenResponse
{
    public string AccessToken { get; init; }

    public string TokenType { get; init; } = "Bearer";

    public DateTime ExpiresAt { get; init; }

    public CustomerInfoDto Customer { get; init; }
}

public record RegisterResponse
{
    /// <summary>
    /// standard (signed in), email_validation (account activation e-mail sent) or admin_approval
    /// </summary>
    public string Status { get; init; }

    public TokenResponse Token { get; init; }
}

#endregion

#region Store

public record StoreInfoDto
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string Url { get; init; }

    public bool PricesIncludeTax { get; init; }

    public bool AnonymousCheckoutAllowed { get; init; }

    public LanguageDto WorkingLanguage { get; init; }

    public CurrencyDto WorkingCurrency { get; init; }

    public IList<LanguageDto> Languages { get; init; } = [];

    public IList<CurrencyDto> Currencies { get; init; } = [];

    public AppThemeDto Theme { get; init; }
}

public record LanguageDto(int Id, string Name, string Culture, bool Rtl);

public record CurrencyDto(int Id, string Name, string Code, decimal Rate);

public record AppThemeDto(string PrimaryColor, string SecondaryColor, string LogoUrl);

#endregion

#region Catalog

public record CategoryDto
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public string SeName { get; init; }

    public int ParentCategoryId { get; init; }

    public int DisplayOrder { get; init; }

    public string ImageUrl { get; init; }
}

public record PriceDto
{
    /// <summary>
    /// Price in the working currency, including or excluding tax according to the store and customer settings
    /// </summary>
    public decimal? Value { get; init; }

    public decimal? OldValue { get; init; }

    public string Formatted { get; init; }

    public string OldFormatted { get; init; }

    public string CurrencyCode { get; init; }

    public bool IncludesTax { get; init; }

    /// <summary>
    /// False when prices are hidden (ACL "Display prices", "Call for price", customer-entered price)
    /// </summary>
    public bool Available { get; init; }
}

public record ProductSummaryDto
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string ShortDescription { get; init; }

    public string SeName { get; init; }

    public string Sku { get; init; }

    public PriceDto Price { get; init; }

    public string ImageUrl { get; init; }

    public bool CanBePurchased { get; init; }
}

public record ProductDetailDto : ProductSummaryDto
{
    public string FullDescription { get; init; }

    public string StockAvailability { get; init; }

    public bool HasRequiredAttributes { get; init; }

    public int OrderMinimumQuantity { get; init; }

    public int OrderMaximumQuantity { get; init; }

    public IList<string> Images { get; init; } = [];

    public IList<int> CategoryIds { get; init; } = [];
}

#endregion

#region Cart

public record AddCartItemRequest(int ProductId, int Quantity = 1);

public record UpdateCartItemRequest(int Quantity);

public record CartItemDto
{
    public int Id { get; init; }

    public int ProductId { get; init; }

    public string ProductName { get; init; }

    public string ImageUrl { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public string UnitPriceFormatted { get; init; }

    public decimal SubTotal { get; init; }

    public string SubTotalFormatted { get; init; }

    public IList<string> Warnings { get; init; } = [];
}

public record CartDto
{
    public IList<CartItemDto> Items { get; init; } = [];

    public int TotalQuantity { get; init; }

    public decimal SubTotal { get; init; }

    public string SubTotalFormatted { get; init; }

    public decimal? Shipping { get; init; }

    public string ShippingFormatted { get; init; }

    public decimal Tax { get; init; }

    public string TaxFormatted { get; init; }

    /// <summary>
    /// Null until shipping can be computed (select a shipping option at checkout)
    /// </summary>
    public decimal? Total { get; init; }

    public string TotalFormatted { get; init; }

    public string CurrencyCode { get; init; }

    public IList<string> Warnings { get; init; } = [];
}

#endregion

#region Account

public record CustomerInfoDto
{
    public int Id { get; init; }

    public bool IsGuest { get; init; }

    public string Email { get; init; }

    public string Username { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string Phone { get; init; }
}

public record UpdateAccountRequest(string FirstName, string LastName, string Phone);

public record ChangePasswordRequest(string OldPassword, string NewPassword);

public record AddressDto
{
    public int Id { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string Email { get; init; }

    public string Company { get; init; }

    public int? CountryId { get; init; }

    public string CountryName { get; init; }

    public int? StateProvinceId { get; init; }

    public string City { get; init; }

    public string Address1 { get; init; }

    public string Address2 { get; init; }

    public string ZipPostalCode { get; init; }

    public string PhoneNumber { get; init; }
}

public record CountryDto(int Id, string Name, string TwoLetterIsoCode, bool AllowsShipping, bool AllowsBilling);

public record StateProvinceDto(int Id, string Name, string Abbreviation);

#endregion

#region Orders and checkout

public record OrderSummaryDto
{
    public int Id { get; init; }

    public string CustomOrderNumber { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public string OrderStatus { get; init; }

    public string PaymentStatus { get; init; }

    public string ShippingStatus { get; init; }

    public decimal OrderTotal { get; init; }

    public string OrderTotalFormatted { get; init; }

    public string CurrencyCode { get; init; }
}

public record OrderItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Total, string AttributeDescription);

public record OrderDetailDto : OrderSummaryDto
{
    public string PaymentMethod { get; init; }

    public string ShippingMethod { get; init; }

    public decimal OrderSubtotal { get; init; }

    public decimal OrderShipping { get; init; }

    public decimal OrderTax { get; init; }

    public AddressDto BillingAddress { get; init; }

    public AddressDto ShippingAddress { get; init; }

    public IList<OrderItemDto> Items { get; init; } = [];
}

public record ShippingOptionDto(string Id, string Name, string Description, decimal Rate, string RateFormatted, string ProviderSystemName);

public record PaymentMethodDto(string SystemName, string Name, string Description, bool SupportedByApi);

public record CheckoutRequest
{
    public int BillingAddressId { get; init; }

    /// <summary>
    /// Required when the cart needs shipping
    /// </summary>
    public int? ShippingAddressId { get; init; }

    /// <summary>
    /// Id of an option returned by GET checkout/shipping-options
    /// </summary>
    public string ShippingOptionId { get; init; }

    public string PaymentMethodSystemName { get; init; }

    /// <summary>
    /// Checkout attributes (gift wrapping...) returned by GET checkout/attributes
    /// </summary>
    public IList<CheckoutAttributeSelection> Attributes { get; init; } = [];
}

/// <summary>
/// Choice for a checkout attribute: valueIds for lists and checkboxes, text for text boxes and dates (yyyy-MM-dd)
/// </summary>
public record CheckoutAttributeSelection
{
    public int AttributeId { get; init; }

    public IList<int> ValueIds { get; init; } = [];

    public string Text { get; init; }
}

public record CheckoutAttributeValueDto(int Id, string Name, decimal PriceAdjustment, string PriceAdjustmentFormatted, bool IsPreSelected);

public record CheckoutAttributeDto(int Id, string Name, string TextPrompt, bool IsRequired, string ControlType, IList<CheckoutAttributeValueDto> Values);

public record CheckoutResponse(int OrderId, string CustomOrderNumber, decimal OrderTotal, string OrderTotalFormatted, string PaymentStatus);

#endregion
