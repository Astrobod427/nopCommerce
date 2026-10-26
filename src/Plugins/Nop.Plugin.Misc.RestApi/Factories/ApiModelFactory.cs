using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Web.Factories;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Misc.RestApi.Factories;

/// <summary>
/// Prepares API models. Prices and totals come from the nopCommerce web model factories and services,
/// so the API shows exactly what the web store shows (taxes, discounts, currency, ACL "Display prices").
/// </summary>
public class ApiModelFactory
{
    #region Fields

    protected readonly IAddressService _addressService;
    protected readonly ICategoryService _categoryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ICountryService _countryService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    protected readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IWorkContext _workContext;
    protected readonly MediaSettings _mediaSettings;

    #endregion

    #region Ctor

    public ApiModelFactory(IAddressService addressService,
        ICategoryService categoryService,
        ICurrencyService currencyService,
        ICountryService countryService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPictureService pictureService,
        IProductAttributeService productAttributeService,
        IPriceFormatter priceFormatter,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IShoppingCartService shoppingCartService,
        IStateProvinceService stateProvinceService,
        IUrlRecordService urlRecordService,
        IWorkContext workContext,
        MediaSettings mediaSettings)
    {
        _addressService = addressService;
        _categoryService = categoryService;
        _currencyService = currencyService;
        _countryService = countryService;
        _customerService = customerService;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _pictureService = pictureService;
        _productAttributeService = productAttributeService;
        _priceFormatter = priceFormatter;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _shoppingCartService = shoppingCartService;
        _stateProvinceService = stateProvinceService;
        _urlRecordService = urlRecordService;
        _workContext = workContext;
        _mediaSettings = mediaSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<bool> PricesIncludeTaxAsync()
    {
        return await _workContext.GetTaxDisplayTypeAsync() == Nop.Core.Domain.Tax.TaxDisplayType.IncludingTax;
    }

    #endregion

    #region Customers and addresses

    public virtual async Task<CustomerInfoDto> PrepareCustomerInfoAsync(Customer customer)
    {
        return new CustomerInfoDto
        {
            Id = customer.Id,
            IsGuest = await _customerService.IsGuestAsync(customer),
            Email = customer.Email,
            Username = customer.Username,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone
        };
    }

    public virtual async Task<AddressDto> PrepareAddressAsync(Address address)
    {
        if (address == null)
            return null;

        var country = address.CountryId.HasValue ? await _countryService.GetCountryByIdAsync(address.CountryId.Value) : null;

        return new AddressDto
        {
            Id = address.Id,
            FirstName = address.FirstName,
            LastName = address.LastName,
            Email = address.Email,
            Company = address.Company,
            CountryId = address.CountryId,
            CountryName = country == null ? null : await _localizationService.GetLocalizedAsync(country, c => c.Name),
            StateProvinceId = address.StateProvinceId,
            City = address.City,
            Address1 = address.Address1,
            Address2 = address.Address2,
            ZipPostalCode = address.ZipPostalCode,
            PhoneNumber = address.PhoneNumber
        };
    }

    #endregion

    #region Catalog

    public virtual async Task<CategoryDto> PrepareCategoryAsync(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = await _localizationService.GetLocalizedAsync(category, c => c.Name),
            Description = await _localizationService.GetLocalizedAsync(category, c => c.Description),
            SeName = await _urlRecordService.GetSeNameAsync(category),
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            ImageUrl = category.PictureId > 0 ? await _pictureService.GetPictureUrlAsync(category.PictureId, _mediaSettings.CategoryThumbPictureSize, false) : null
        };
    }

    protected virtual PriceDto PreparePrice(Nop.Web.Models.Catalog.ProductPriceModel price, bool includesTax)
    {
        var available = price is { HidePrices: false, CallForPrice: false, CustomerEntersPrice: false } && price.PriceValue.HasValue;

        return new PriceDto
        {
            Available = available,
            Value = available ? price.PriceValue : null,
            OldValue = available ? price.OldPriceValue : null,
            Formatted = price?.PriceWithDiscount ?? price?.Price,
            OldFormatted = available ? price.OldPrice : null,
            CurrencyCode = price?.CurrencyCode,
            IncludesTax = includesTax
        };
    }

    public virtual async Task<IList<ProductSummaryDto>> PrepareProductSummariesAsync(IList<Product> products)
    {
        var includesTax = await PricesIncludeTaxAsync();
        var models = await _productModelFactory.PrepareProductOverviewModelsAsync(products);

        return models.Select(model => new ProductSummaryDto
        {
            Id = model.Id,
            Name = model.Name,
            ShortDescription = model.ShortDescription,
            SeName = model.SeName,
            Sku = model.Sku,
            Price = PreparePrice(model.ProductPrice, includesTax),
            ImageUrl = model.PictureModels.FirstOrDefault()?.ImageUrl,
            CanBePurchased = !model.ProductPrice.DisableBuyButton
        }).ToList();
    }

    public virtual async Task<ProductDetailDto> PrepareProductDetailAsync(Product product)
    {
        var summary = (await PrepareProductSummariesAsync([product])).First();

        var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);
        var images = new List<string>();
        foreach (var picture in pictures)
            images.Add((await _pictureService.GetPictureUrlAsync(picture, _mediaSettings.ProductDetailsPictureSize)).Url);

        var attributes = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);

        return new ProductDetailDto
        {
            Id = summary.Id,
            Name = summary.Name,
            ShortDescription = summary.ShortDescription,
            SeName = summary.SeName,
            Sku = summary.Sku,
            Price = summary.Price,
            ImageUrl = summary.ImageUrl,
            CanBePurchased = summary.CanBePurchased,
            FullDescription = await _localizationService.GetLocalizedAsync(product, p => p.FullDescription),
            StockAvailability = await _productService.FormatStockMessageAsync(product, string.Empty),
            HasRequiredAttributes = attributes.Any(a => a.IsRequired),
            OrderMinimumQuantity = product.OrderMinimumQuantity,
            OrderMaximumQuantity = product.OrderMaximumQuantity,
            Images = images,
            CategoryIds = (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id)).Select(pc => pc.CategoryId).ToList()
        };
    }

    #endregion

    #region Cart

    public virtual async Task<CartDto> PrepareCartAsync(IList<ShoppingCartItem> cart)
    {
        var currency = await _workContext.GetWorkingCurrencyAsync();
        var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), cart);

        var items = model.Items.Select(item => new CartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ImageUrl = item.Picture?.ImageUrl,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPriceValue,
            UnitPriceFormatted = item.UnitPrice,
            SubTotal = item.SubTotalValue,
            SubTotalFormatted = item.SubTotal,
            Warnings = item.Warnings
        }).ToList();

        if (!cart.Any())
            return new CartDto { CurrencyCode = currency.CurrencyCode };

        //same totals as the "Order totals" block of the web cart
        var includesTax = await PricesIncludeTaxAsync();
        var (_, _, subTotalWithoutDiscount, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, includesTax);
        var subTotal = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(subTotalWithoutDiscount, currency);
        var (shippingBase, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(cart, includesTax);
        decimal? shipping = shippingBase.HasValue ? await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shippingBase.Value, currency) : null;
        var (taxBase, _) = await _orderTotalCalculationService.GetTaxTotalAsync(cart);
        var tax = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(taxBase, currency);
        var (totalBase, _, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart);
        decimal? total = totalBase.HasValue ? await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(totalBase.Value, currency) : null;

        return new CartDto
        {
            Items = items,
            TotalQuantity = items.Sum(i => i.Quantity),
            SubTotal = subTotal,
            SubTotalFormatted = await _priceFormatter.FormatPriceAsync(subTotal, true, false),
            Shipping = shipping,
            ShippingFormatted = shipping.HasValue ? await _priceFormatter.FormatPriceAsync(shipping.Value, true, false) : null,
            Tax = tax,
            TaxFormatted = await _priceFormatter.FormatPriceAsync(tax, true, false),
            Total = total,
            TotalFormatted = total.HasValue ? await _priceFormatter.FormatPriceAsync(total.Value, true, false) : null,
            CurrencyCode = currency.CurrencyCode,
            Warnings = model.Warnings
        };
    }

    #endregion

    #region Orders

    public virtual async Task<OrderSummaryDto> PrepareOrderSummaryAsync(Order order)
    {
        return new OrderSummaryDto
        {
            Id = order.Id,
            CustomOrderNumber = order.CustomOrderNumber,
            CreatedOnUtc = order.CreatedOnUtc,
            OrderStatus = order.OrderStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            ShippingStatus = order.ShippingStatus.ToString(),
            OrderTotal = order.OrderTotal * order.CurrencyRate,
            OrderTotalFormatted = await _priceFormatter.FormatPriceAsync(order.OrderTotal * order.CurrencyRate, true, order.CustomerCurrencyCode, false, (await _workContext.GetWorkingLanguageAsync()).Id),
            CurrencyCode = order.CustomerCurrencyCode
        };
    }

    public virtual async Task<OrderDetailDto> PrepareOrderDetailAsync(Order order, IList<OrderItem> items)
    {
        var summary = await PrepareOrderSummaryAsync(order);
        var includesTax = order.CustomerTaxDisplayType == Nop.Core.Domain.Tax.TaxDisplayType.IncludingTax;
        var itemDtos = new List<OrderItemDto>();
        foreach (var item in items)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            itemDtos.Add(new OrderItemDto(item.Id, item.ProductId,
                product == null ? null : await _localizationService.GetLocalizedAsync(product, p => p.Name),
                item.Quantity,
                (includesTax ? item.UnitPriceInclTax : item.UnitPriceExclTax) * order.CurrencyRate,
                (includesTax ? item.PriceInclTax : item.PriceExclTax) * order.CurrencyRate,
                item.AttributeDescription));
        }

        return new OrderDetailDto
        {
            Id = summary.Id,
            CustomOrderNumber = summary.CustomOrderNumber,
            CreatedOnUtc = summary.CreatedOnUtc,
            OrderStatus = summary.OrderStatus,
            PaymentStatus = summary.PaymentStatus,
            ShippingStatus = summary.ShippingStatus,
            OrderTotal = summary.OrderTotal,
            OrderTotalFormatted = summary.OrderTotalFormatted,
            CurrencyCode = summary.CurrencyCode,
            PaymentMethod = order.PaymentMethodSystemName,
            ShippingMethod = order.ShippingMethod,
            OrderSubtotal = (includesTax ? order.OrderSubtotalInclTax : order.OrderSubtotalExclTax) * order.CurrencyRate,
            OrderShipping = (includesTax ? order.OrderShippingInclTax : order.OrderShippingExclTax) * order.CurrencyRate,
            OrderTax = order.OrderTax * order.CurrencyRate,
            BillingAddress = await PrepareAddressAsync(await _addressService.GetAddressByIdAsync(order.BillingAddressId)),
            ShippingAddress = order.ShippingAddressId.HasValue ? await PrepareAddressAsync(await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value)) : null,
            Items = itemDtos
        };
    }

    #endregion
}
