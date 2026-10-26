using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Storefront: the account of the signed-in customer. Every endpoint works on the customer of the token only.
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/me")]
[ApiAuthorize(ApiTokenKind.Storefront)]
public class StorefrontAccountController : BaseApiController
{
    #region Fields

    protected readonly ApiModelFactory _apiModelFactory;
    protected readonly CustomerSettings _customerSettings;
    protected readonly IAddressService _addressService;
    protected readonly ICustomerRegistrationService _customerRegistrationService;
    protected readonly ICustomerService _customerService;
    protected readonly IOrderService _orderService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public StorefrontAccountController(ApiModelFactory apiModelFactory,
        CustomerSettings customerSettings,
        IAddressService addressService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerService customerService,
        IOrderService orderService,
        IStoreContext storeContext,
        IWorkContext workContext,
        RestApiSettings settings)
    {
        _apiModelFactory = apiModelFactory;
        _customerSettings = customerSettings;
        _addressService = addressService;
        _customerRegistrationService = customerRegistrationService;
        _customerService = customerService;
        _orderService = orderService;
        _storeContext = storeContext;
        _workContext = workContext;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<IActionResult> RequireRegisteredAsync(Customer customer)
    {
        return await _customerService.IsRegisteredAsync(customer)
            ? null
            : ApiError(StatusCodes.Status403Forbidden, "registration_required", "Sign in with a customer account first.");
    }

    protected static void MapAddress(AddressDto dto, Address address)
    {
        address.FirstName = dto.FirstName?.Trim();
        address.LastName = dto.LastName?.Trim();
        address.Email = dto.Email?.Trim();
        address.Company = dto.Company?.Trim();
        address.CountryId = dto.CountryId is > 0 ? dto.CountryId : null;
        address.StateProvinceId = dto.StateProvinceId is > 0 ? dto.StateProvinceId : null;
        address.City = dto.City?.Trim();
        address.Address1 = dto.Address1?.Trim();
        address.Address2 = dto.Address2?.Trim();
        address.ZipPostalCode = dto.ZipPostalCode?.Trim();
        address.PhoneNumber = dto.PhoneNumber?.Trim();
    }

    #endregion

    #region Profile

    [HttpGet("")]
    public virtual async Task<IActionResult> Get()
    {
        return ApiOk(await _apiModelFactory.PrepareCustomerInfoAsync(await _workContext.GetCurrentCustomerAsync()));
    }

    [HttpPut("")]
    public virtual async Task<IActionResult> Update([FromBody] UpdateAccountRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await RequireRegisteredAsync(customer) is { } denied)
            return denied;

        if (request == null)
            return ApiBadRequest("A body is required.");

        customer.FirstName = request.FirstName?.Trim();
        customer.LastName = request.LastName?.Trim();
        if (_customerSettings.PhoneEnabled)
            customer.Phone = request.Phone?.Trim();
        await _customerService.UpdateCustomerAsync(customer);

        return ApiOk(await _apiModelFactory.PrepareCustomerInfoAsync(customer));
    }

    /// <summary>
    /// Changes the password; every session of the customer (this one included) is closed
    /// </summary>
    [HttpPost("password")]
    public virtual async Task<IActionResult> ChangePassword([FromBody] Models.ChangePasswordRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await RequireRegisteredAsync(customer) is { } denied)
            return denied;

        var result = await _customerRegistrationService.ChangePasswordAsync(new Nop.Services.Customers.ChangePasswordRequest(customer.Email,
            true, _customerSettings.DefaultPasswordFormat, request?.NewPassword, request?.OldPassword));
        if (!result.Success)
            return ApiBadRequest("The password could not be changed.", result.Errors);

        return NoContent();
    }

    #endregion

    #region Addresses

    [HttpGet("addresses")]
    public virtual async Task<IActionResult> Addresses()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var addresses = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);

        return ApiOk(await addresses.SelectAwait(async a => await _apiModelFactory.PrepareAddressAsync(a)).ToListAsync());
    }

    [HttpPost("addresses")]
    public virtual async Task<IActionResult> AddAddress([FromBody] AddressDto request)
    {
        if (request == null)
            return ApiBadRequest("A body is required.");

        var customer = await _workContext.GetCurrentCustomerAsync();
        var address = new Address { CreatedOnUtc = DateTime.UtcNow };
        MapAddress(request, address);
        if (!await _addressService.IsAddressValidAsync(address))
            return ApiBadRequest("The address is incomplete or invalid for the store's address settings (first and last name, e-mail and the required fields; stateProvinceId when the country has states, see GET countries/{id}/states).");

        await _addressService.InsertAddressAsync(address);
        await _customerService.InsertCustomerAddressAsync(customer, address);

        return ApiCreated(await _apiModelFactory.PrepareAddressAsync(address));
    }

    [HttpPut("addresses/{id:int}")]
    public virtual async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressDto request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var address = await _customerService.GetCustomerAddressAsync(customer.Id, id);
        if (address == null)
            return ApiNotFound("Address");

        if (request == null)
            return ApiBadRequest("A body is required.");

        MapAddress(request, address);
        if (!await _addressService.IsAddressValidAsync(address))
            return ApiBadRequest("The address is incomplete or invalid for the store's address settings (first and last name, e-mail and the required fields; stateProvinceId when the country has states, see GET countries/{id}/states).");

        await _addressService.UpdateAddressAsync(address);

        return ApiOk(await _apiModelFactory.PrepareAddressAsync(address));
    }

    [HttpDelete("addresses/{id:int}")]
    public virtual async Task<IActionResult> DeleteAddress(int id)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var address = await _customerService.GetCustomerAddressAsync(customer.Id, id);
        if (address == null)
            return ApiNotFound("Address");

        await _customerService.RemoveCustomerAddressAsync(customer, address);
        await _customerService.UpdateCustomerAsync(customer);
        await _addressService.DeleteAddressAsync(address);

        return NoContent();
    }

    #endregion

    #region Orders

    [HttpGet("orders")]
    public virtual async Task<IActionResult> Orders([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var orders = await _orderService.SearchOrdersAsync(storeId: store.Id, customerId: customer.Id,
            pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings));

        return ApiOk(ToPaged(orders, await orders.SelectAwait(async o => await _apiModelFactory.PrepareOrderSummaryAsync(o)).ToListAsync()));
    }

    [HttpGet("orders/{id:int}")]
    public virtual async Task<IActionResult> Order(int id)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var order = await _orderService.GetOrderByIdAsync(id);
        //only the customer's own orders (same rule as the web "Order details" page)
        if (order == null || order.Deleted || order.CustomerId != customer.Id)
            return ApiNotFound("Order");

        return ApiOk(await _apiModelFactory.PrepareOrderDetailAsync(order, await _orderService.GetOrderItemsAsync(order.Id)));
    }

    #endregion
}
