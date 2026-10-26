using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Storefront: the shopping cart of the signed-in customer (or guest session)
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/cart")]
[ApiAuthorize(ApiTokenKind.Storefront, StandardPermission.PublicStore.ENABLE_SHOPPING_CART)]
public class StorefrontCartController : BaseApiController
{
    #region Fields

    protected readonly ApiModelFactory _apiModelFactory;
    protected readonly IProductService _productService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public StorefrontCartController(ApiModelFactory apiModelFactory,
        IProductService productService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _apiModelFactory = apiModelFactory;
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<IList<ShoppingCartItem>> GetCartAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        return await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
    }

    protected virtual async Task<IActionResult> CartResultAsync(IList<string> warnings = null)
    {
        var cart = await _apiModelFactory.PrepareCartAsync(await GetCartAsync());
        if (warnings?.Any() == true)
            return ApiBadRequest("The cart could not be updated.", warnings);

        return ApiOk(cart);
    }

    #endregion

    #region Methods

    [HttpGet("")]
    public virtual async Task<IActionResult> Get()
    {
        return await CartResultAsync();
    }

    /// <summary>
    /// Adds a product (products with required attributes must be ordered on the web store for now)
    /// </summary>
    [HttpPost("items")]
    public virtual async Task<IActionResult> Add([FromBody] AddCartItemRequest request)
    {
        if (request == null || request.Quantity <= 0)
            return ApiBadRequest("A product and a positive quantity are required.");

        var product = await _productService.GetProductByIdAsync(request.ProductId);
        if (product == null || product.Deleted)
            return ApiNotFound("Product");

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        //the cart service checks publication, ACL, store mapping, stock, quantities and required attributes
        var warnings = await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart, store.Id, quantity: request.Quantity);

        return await CartResultAsync(warnings);
    }

    [HttpPut("items/{id:int}")]
    public virtual async Task<IActionResult> Update(int id, [FromBody] UpdateCartItemRequest request)
    {
        var item = (await GetCartAsync()).FirstOrDefault(i => i.Id == id);
        if (item == null)
            return ApiNotFound("Cart item");

        if (request == null || request.Quantity <= 0)
        {
            await _shoppingCartService.DeleteShoppingCartItemAsync(item);
            return await CartResultAsync();
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(customer, item.Id, item.AttributesXml, item.CustomerEnteredPrice,
            item.RentalStartDateUtc, item.RentalEndDateUtc, request.Quantity);

        return await CartResultAsync(warnings);
    }

    [HttpDelete("items/{id:int}")]
    public virtual async Task<IActionResult> Remove(int id)
    {
        var item = (await GetCartAsync()).FirstOrDefault(i => i.Id == id);
        if (item == null)
            return ApiNotFound("Cart item");

        await _shoppingCartService.DeleteShoppingCartItemAsync(item);

        return await CartResultAsync();
    }

    [HttpDelete("")]
    public virtual async Task<IActionResult> Clear()
    {
        foreach (var item in await GetCartAsync())
            await _shoppingCartService.DeleteShoppingCartItemAsync(item);

        return await CartResultAsync();
    }

    #endregion
}
