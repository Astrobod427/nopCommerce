using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.RestApi.Api.Filters;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Back office: customers, with the permissions of the admin area (Customers > Customers)
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/admin/customers")]
[ApiAuthorize(ApiTokenKind.BackOffice)]
public class BackOfficeCustomersController : BaseApiController
{
    #region Fields

    protected readonly BackOfficeModelFactory _backOfficeModelFactory;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IWorkContext _workContext;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public BackOfficeCustomersController(BackOfficeModelFactory backOfficeModelFactory,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IWorkContext workContext,
        RestApiSettings settings)
    {
        _backOfficeModelFactory = backOfficeModelFactory;
        _customerActivityService = customerActivityService;
        _customerService = customerService;
        _localizationService = localizationService;
        _workContext = workContext;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetCustomerAsync(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer is { Deleted: false } ? customer : null;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Searches registered customers (guests excluded)
    /// </summary>
    [HttpGet("")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Customers.CUSTOMERS_VIEW)]
    public virtual async Task<IActionResult> List([FromQuery] string email, [FromQuery] string lastName, [FromQuery] bool? active,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 0)
    {
        var registered = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
        var customers = await _customerService.GetAllCustomersAsync(customerRoleIds: registered == null ? null : [registered.Id],
            email: email, lastName: lastName, isActive: active,
            pageIndex: Math.Max(pageIndex, 0), pageSize: ClampPageSize(pageSize, _settings));

        var items = new List<AdminCustomerDto>();
        foreach (var customer in customers)
            items.Add(await _backOfficeModelFactory.PrepareCustomerAsync(customer));

        return ApiOk(ToPaged(customers, items));
    }

    [HttpGet("{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Customers.CUSTOMERS_VIEW)]
    public virtual async Task<IActionResult> Get(int id)
    {
        var customer = await GetCustomerAsync(id);
        if (customer == null)
            return ApiNotFound("Customer");

        return ApiOk(await _backOfficeModelFactory.PrepareCustomerAsync(customer));
    }

    /// <summary>
    /// Updates a customer; omitted properties are left unchanged. Deactivating a customer ends their API sessions.
    /// </summary>
    [HttpPut("{id:int}")]
    [ApiAuthorize(ApiTokenKind.BackOffice, StandardPermission.Customers.CUSTOMERS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Update(int id, [FromBody] AdminCustomerRequest request)
    {
        var customer = await GetCustomerAsync(id);
        if (customer == null)
            return ApiNotFound("Customer");

        if (request == null)
            return ApiBadRequest("A request body is required.");

        //an API client cannot raise its rights by taking over an administrator account
        var current = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsAdminAsync(customer) && !await _customerService.IsAdminAsync(current))
            return ApiError(StatusCodes.Status403Forbidden, "forbidden", "Only administrators can edit administrator accounts.");

        if (request.Active == false && customer.Id == current.Id)
            return ApiBadRequest("The account of the API client cannot be deactivated through the API.");

        if (request.FirstName != null)
            customer.FirstName = request.FirstName;
        if (request.LastName != null)
            customer.LastName = request.LastName;
        if (request.Company != null)
            customer.Company = request.Company;
        if (request.Phone != null)
            customer.Phone = request.Phone;
        if (request.Active.HasValue)
            customer.Active = request.Active.Value;
        await _customerService.UpdateCustomerAsync(customer);

        await _customerActivityService.InsertActivityAsync("EditCustomer",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditCustomer"), customer.Id), customer);

        return ApiOk(await _backOfficeModelFactory.PrepareCustomerAsync(customer));
    }

    #endregion
}
