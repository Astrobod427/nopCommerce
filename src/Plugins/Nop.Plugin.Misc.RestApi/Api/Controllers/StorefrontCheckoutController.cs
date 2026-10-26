using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Attributes;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Storefront: checkout of the cart. Orders are placed by the nopCommerce order processing service,
/// exactly as on the web store (stock, discounts, taxes, minimum amounts, e-mails, events).
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/checkout")]
[ApiAuthorize(ApiTokenKind.Storefront, StandardPermission.PublicStore.ENABLE_SHOPPING_CART)]
public class StorefrontCheckoutController : BaseApiController
{
    #region Constants

    /// <summary>
    /// Separator of a shipping option id: "{name}___{rate computation method}" (same as the web checkout)
    /// </summary>
    protected const string SHIPPING_OPTION_SEPARATOR = "___";

    #endregion

    #region Fields

    protected readonly IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeParser;
    protected readonly IAttributeService<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IProductService _productService;
    protected readonly IPaymentService _paymentService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IShippingService _shippingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IWorkContext _workContext;
    protected readonly OrderSettings _orderSettings;

    #endregion

    #region Ctor

    public StorefrontCheckoutController(IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IAttributeService<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IOrderProcessingService orderProcessingService,
        IPaymentPluginManager paymentPluginManager,
        IProductService productService,
        IPaymentService paymentService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPriceFormatter priceFormatter,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IWorkContext workContext,
        OrderSettings orderSettings)
    {
        _checkoutAttributeParser = checkoutAttributeParser;
        _checkoutAttributeService = checkoutAttributeService;
        _currencyService = currencyService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _logger = logger;
        _orderProcessingService = orderProcessingService;
        _paymentPluginManager = paymentPluginManager;
        _productService = productService;
        _paymentService = paymentService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _priceFormatter = priceFormatter;
        _shippingService = shippingService;
        _shoppingCartService = shoppingCartService;
        _staticCacheManager = staticCacheManager;
        _storeContext = storeContext;
        _storeMappingService = storeMappingService;
        _workContext = workContext;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<(Customer customer, int storeId, IList<ShoppingCartItem> cart, IActionResult error)> PrepareCheckoutAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (_orderSettings.CheckoutDisabled)
            return (customer, store.Id, cart, ApiError(StatusCodes.Status403Forbidden, "checkout_disabled", await _localizationService.GetResourceAsync("Checkout.Disabled")));

        if (!cart.Any())
            return (customer, store.Id, cart, ApiBadRequest(await _localizationService.GetResourceAsync("ShoppingCart.CartIsEmpty")));

        if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
            return (customer, store.Id, cart, ApiError(StatusCodes.Status403Forbidden, "registration_required", await _localizationService.GetResourceAsync("Checkout.AnonymousNotAllowed")));

        return (customer, store.Id, cart, null);
    }

    /// <summary>
    /// A payment method can be used through the API when it needs no redirection, no button and no payment form input
    /// </summary>
    protected virtual async Task<bool> IsSupportedByApiAsync(IPaymentMethod method)
    {
        if (method.PaymentMethodType != PaymentMethodType.Standard)
            return false;

        try
        {
            var errors = await method.ValidatePaymentFormAsync(new FormCollection(new Dictionary<string, StringValues>()));
            return !errors.Any();
        }
        catch
        {
            return false;
        }
    }

    protected virtual async Task<IList<CheckoutAttribute>> GetCheckoutAttributesAsync(int storeId, IList<ShoppingCartItem> cart)
    {
        var excludeShippableAttributes = !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart);
        return await _checkoutAttributeService.GetAllAttributesAsync(_staticCacheManager, _storeMappingService, storeId, excludeShippableAttributes);
    }

    /// <summary>
    /// Builds the checkout attributes XML from the request, as the web cart page does from its form
    /// </summary>
    protected virtual async Task<string> ParseCheckoutAttributesAsync(int storeId, IList<ShoppingCartItem> cart, IList<CheckoutAttributeSelection> selections)
    {
        var attributesXml = string.Empty;
        var checkoutAttributes = await GetCheckoutAttributesAsync(storeId, cart);
        foreach (var attribute in checkoutAttributes)
        {
            var selection = selections?.FirstOrDefault(s => s.AttributeId == attribute.Id);
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                case AttributeControlType.Checkboxes:
                {
                    if (selection == null)
                        break;

                    var valueIds = (await _checkoutAttributeService.GetAttributeValuesAsync(attribute.Id)).Select(v => v.Id).ToHashSet();
                    var selected = selection.ValueIds?.Where(valueIds.Contains).Distinct() ?? [];
                    if (attribute.AttributeControlType != AttributeControlType.Checkboxes)
                        selected = selected.Take(1);
                    foreach (var valueId in selected)
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, valueId.ToString());
                    break;
                }
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    foreach (var value in (await _checkoutAttributeService.GetAttributeValuesAsync(attribute.Id)).Where(v => v.IsPreSelected))
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, value.Id.ToString());
                    break;
                }
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    if (!string.IsNullOrWhiteSpace(selection?.Text))
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, selection.Text.Trim());
                    break;
                }
                case AttributeControlType.Datepicker:
                {
                    if (DateTime.TryParseExact(selection?.Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, date.ToString("D"));
                    break;
                }
                //file uploads need the web store
            }
        }

        //conditional attributes
        foreach (var attribute in checkoutAttributes)
        {
            var conditionMet = await _checkoutAttributeParser.IsConditionMetAsync(attribute.ConditionAttributeXml, attributesXml);
            if (conditionMet.HasValue && !conditionMet.Value)
                attributesXml = _checkoutAttributeParser.RemoveAttribute(attributesXml, attribute.Id);
        }

        return attributesXml;
    }

    protected virtual async Task<IList<ShippingOption>> GetShippingOptionsAsync(Customer customer, int storeId, IList<ShoppingCartItem> cart, Nop.Core.Domain.Common.Address address)
    {
        var response = await _shippingService.GetShippingOptionsAsync(cart, address, customer, storeId: storeId);
        return response.Success ? response.ShippingOptions : [];
    }

    #endregion

    #region Methods

    /// <summary>
    /// Checkout attributes of the store (gift wrapping...); send the choices in POST checkout
    /// </summary>
    [HttpGet("attributes")]
    public virtual async Task<IActionResult> Attributes()
    {
        var (_, storeId, cart, error) = await PrepareCheckoutAsync();
        if (error != null)
            return error;

        var language = await _workContext.GetWorkingLanguageAsync();
        var currency = await _workContext.GetWorkingCurrencyAsync();
        var result = new List<CheckoutAttributeDto>();
        foreach (var attribute in await GetCheckoutAttributesAsync(storeId, cart))
        {
            var values = new List<CheckoutAttributeValueDto>();
            foreach (var value in await _checkoutAttributeService.GetAttributeValuesAsync(attribute.Id))
            {
                var adjustment = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(value.PriceAdjustment, currency);
                values.Add(new CheckoutAttributeValueDto(value.Id, await _localizationService.GetLocalizedAsync(value, v => v.Name, language.Id),
                    adjustment, await _priceFormatter.FormatPriceAsync(adjustment), value.IsPreSelected));
            }

            result.Add(new CheckoutAttributeDto(attribute.Id, await _localizationService.GetLocalizedAsync(attribute, a => a.Name, language.Id),
                await _localizationService.GetLocalizedAsync(attribute, a => a.TextPrompt, language.Id),
                attribute.IsRequired, attribute.AttributeControlType.ToString(), values));
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Shipping options for the cart and a shipping address of the customer
    /// </summary>
    [HttpGet("shipping-options")]
    public virtual async Task<IActionResult> ShippingOptions([FromQuery] int addressId)
    {
        var (customer, storeId, cart, error) = await PrepareCheckoutAsync();
        if (error != null)
            return error;

        if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
            return ApiOk(Array.Empty<ShippingOptionDto>());

        var address = await _customerService.GetCustomerAddressAsync(customer.Id, addressId);
        if (address == null)
            return ApiNotFound("Address");

        var currency = await _workContext.GetWorkingCurrencyAsync();
        var result = new List<ShippingOptionDto>();
        foreach (var option in await GetShippingOptionsAsync(customer, storeId, cart, address))
        {
            var (rateBase, _) = await _orderTotalCalculationService.AdjustShippingRateAsync(option.Rate, cart, option.IsPickupInStore);
            var rate = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(rateBase, currency);
            result.Add(new ShippingOptionDto($"{option.Name}{SHIPPING_OPTION_SEPARATOR}{option.ShippingRateComputationMethodSystemName}",
                option.Name, option.Description, rate, await _priceFormatter.FormatShippingPriceAsync(rate, true), option.ShippingRateComputationMethodSystemName));
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Active payment methods (supportedByApi = can be used with POST checkout)
    /// </summary>
    [HttpGet("payment-methods")]
    public virtual async Task<IActionResult> PaymentMethods()
    {
        var (customer, storeId, _, error) = await PrepareCheckoutAsync();
        if (error != null)
            return error;

        var result = new List<PaymentMethodDto>();
        foreach (var method in await _paymentPluginManager.LoadActivePluginsAsync(customer, storeId))
        {
            result.Add(new PaymentMethodDto(method.PluginDescriptor.SystemName,
                await _localizationService.GetLocalizedFriendlyNameAsync(method, (await _workContext.GetWorkingLanguageAsync()).Id),
                await method.GetPaymentMethodDescriptionAsync(),
                await IsSupportedByApiAsync(method)));
        }

        return ApiOk(result);
    }

    /// <summary>
    /// Places the order of the current cart
    /// </summary>
    [HttpPost("")]
    public virtual async Task<IActionResult> PlaceOrder([FromBody] CheckoutRequest request)
    {
        var (customer, storeId, cart, error) = await PrepareCheckoutAsync();
        if (error != null)
            return error;

        if (request == null)
            return ApiBadRequest("A body is required.");

        //checkout attributes, saved where the order processing service reads them
        var checkoutAttributesXml = await ParseCheckoutAttributesAsync(storeId, cart, request.Attributes);
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CheckoutAttributes, checkoutAttributesXml, storeId);

        //cart warnings (required checkout attributes, stock, quantities, unpublished products...)
        var warnings = await _shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml, true);
        foreach (var item in cart)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            warnings = warnings.Concat(await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer, item.ShoppingCartType, product, storeId,
                item.AttributesXml, item.CustomerEnteredPrice, item.RentalStartDateUtc, item.RentalEndDateUtc, item.Quantity, false, item.Id)).ToList();
        }
        if (warnings.Any())
            return ApiBadRequest("The cart cannot be ordered.", warnings);

        //billing address
        var billingAddress = await _customerService.GetCustomerAddressAsync(customer.Id, request.BillingAddressId);
        if (billingAddress == null)
            return ApiNotFound("Billing address");
        customer.BillingAddressId = billingAddress.Id;

        //shipping
        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            var shippingAddress = await _customerService.GetCustomerAddressAsync(customer.Id, request.ShippingAddressId ?? 0);
            if (shippingAddress == null)
                return ApiNotFound("Shipping address");

            var parts = (request.ShippingOptionId ?? string.Empty).Split(SHIPPING_OPTION_SEPARATOR);
            var option = parts.Length != 2 ? null : (await GetShippingOptionsAsync(customer, storeId, cart, shippingAddress))
                .FirstOrDefault(o => string.Equals(o.Name, parts[0], StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(o.ShippingRateComputationMethodSystemName, parts[1], StringComparison.InvariantCultureIgnoreCase));
            if (option == null)
                return ApiBadRequest("Select a shipping option returned by GET checkout/shipping-options.");

            customer.ShippingAddressId = shippingAddress.Id;
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, option, storeId);
        }
        else
        {
            customer.ShippingAddressId = null;
            await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, storeId);
        }

        await _customerService.UpdateCustomerAsync(customer);

        //payment
        var paymentMethodSystemName = string.Empty;
        if (await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
        {
            var method = await _paymentPluginManager.LoadPluginBySystemNameAsync(request.PaymentMethodSystemName, customer, storeId);
            if (method == null || !_paymentPluginManager.IsPluginActive(method))
                return ApiBadRequest("Select a payment method returned by GET checkout/payment-methods.");

            if (!await IsSupportedByApiAsync(method))
                return ApiError(StatusCodes.Status422UnprocessableEntity, "payment_method_not_supported", "This payment method needs the web store (redirection or payment form).");

            paymentMethodSystemName = method.PluginDescriptor.SystemName;
        }
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, paymentMethodSystemName, storeId);

        try
        {
            var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(new ProcessPaymentRequest
            {
                StoreId = storeId,
                CustomerId = customer.Id,
                PaymentMethodSystemName = paymentMethodSystemName
            });

            if (!placeOrderResult.Success)
                return ApiBadRequest("The order could not be placed.", placeOrderResult.Errors);

            var order = placeOrderResult.PlacedOrder;
            await _paymentService.PostProcessPaymentAsync(new PostProcessPaymentRequest { Order = order });

            var total = order.OrderTotal * order.CurrencyRate;
            return ApiCreated(new CheckoutResponse(order.Id, order.CustomOrderNumber, total,
                await _priceFormatter.FormatPriceAsync(total, true, order.CustomerCurrencyCode, false, (await _workContext.GetWorkingLanguageAsync()).Id),
                order.PaymentStatus.ToString()));
        }
        catch (Exception exception)
        {
            await _logger.WarningAsync($"REST API checkout: {exception.Message}", exception, customer);
            return ApiBadRequest(exception.Message);
        }
    }

    #endregion
}
