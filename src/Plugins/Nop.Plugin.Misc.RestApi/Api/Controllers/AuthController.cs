using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Events;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Plugin.Misc.RestApi.Services;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Sessions: storefront sign-in (guest, customer account, registration) and back-office client credentials
/// </summary>
[Route(RestApiDefaults.ROUTE_PREFIX + "/auth")]
public class AuthController : BaseApiController
{
    #region Fields

    protected readonly ApiModelFactory _apiModelFactory;
    protected readonly ApiTokenService _apiTokenService;
    protected readonly CustomerSettings _customerSettings;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ICustomerRegistrationService _customerRegistrationService;
    protected readonly ICustomerService _customerService;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly IWebHelper _webHelper;
    protected readonly LocalizationSettings _localizationSettings;
    protected readonly MfaService _mfaService;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public AuthController(ApiModelFactory apiModelFactory,
        ApiTokenService apiTokenService,
        CustomerSettings customerSettings,
        ICustomerActivityService customerActivityService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerService customerService,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IWebHelper webHelper,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        MfaService mfaService,
        RestApiSettings settings)
    {
        _apiModelFactory = apiModelFactory;
        _apiTokenService = apiTokenService;
        _customerSettings = customerSettings;
        _customerActivityService = customerActivityService;
        _customerRegistrationService = customerRegistrationService;
        _customerService = customerService;
        _eventPublisher = eventPublisher;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _permissionService = permissionService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _webHelper = webHelper;
        _localizationSettings = localizationSettings;
        _mfaService = mfaService;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual IActionResult StorefrontDisabled()
    {
        return ApiError(StatusCodes.Status404NotFound, "api_disabled", "This part of the API is disabled.");
    }

    protected virtual async Task<TokenResponse> IssueStorefrontTokenAsync(Customer customer)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var lifetime = TimeSpan.FromDays(_settings.StorefrontTokenLifetimeDays > 0 ? _settings.StorefrontTokenLifetimeDays : 30);
        var (token, entity) = await _apiTokenService.IssueTokenAsync(ApiTokenKind.Storefront, customer.Id, store.Id, lifetime);

        return new TokenResponse
        {
            AccessToken = token,
            ExpiresAt = entity.ExpiresOnUtc,
            Customer = await _apiModelFactory.PrepareCustomerInfoAsync(customer)
        };
    }

    /// <summary>
    /// Issues the session of a signed-in customer, with the same bookkeeping as a web sign-in
    /// </summary>
    protected virtual async Task<TokenResponse> CompleteSignInAsync(Customer customer)
    {
        await TakeOverGuestSessionAsync(customer);

        customer.RequireReLogin = false;
        await _customerService.UpdateCustomerAsync(customer);
        await _eventPublisher.PublishAsync(new CustomerLoggedinEvent(customer));
        await _customerActivityService.InsertActivityAsync(customer, "PublicStore.Login", await _localizationService.GetResourceAsync("ActivityLog.PublicStore.Login"), customer);

        return await IssueStorefrontTokenAsync(customer);
    }

    /// <summary>
    /// The password is right and the account uses a second factor: issues a pending session (no access)
    /// </summary>
    protected virtual async Task<IActionResult> MfaChallengeAsync(string login)
    {
        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsernameAsync(login)
            : await _customerService.GetCustomerByEmailAsync(login);
        if (customer == null)
            return ApiError(StatusCodes.Status401Unauthorized, "invalid_credentials", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));

        if (!await _permissionService.AuthorizeAsync(RestApiPermissionConfigManager.USE_STOREFRONT_API, customer))
            return ApiError(StatusCodes.Status403Forbidden, "forbidden", "This account is not allowed to use the API.");

        var provider = await _mfaService.GetActiveProviderAsync(customer);
        if (!_mfaService.IsSupported(provider))
            return ApiError(StatusCodes.Status403Forbidden, "mfa_not_supported", $"The second-factor method '{provider}' of this account is not supported by the API. Sign in on the web store.");

        var store = await _storeContext.GetCurrentStoreAsync();
        var (token, entity) = await _apiTokenService.IssueTokenAsync(ApiTokenKind.MfaPending, customer.Id, store.Id, RestApiDefaults.MfaPendingTokenLifetime);

        return ApiOk(new MfaChallengeResponse { MfaToken = token, Provider = provider, ExpiresAt = entity.ExpiresOnUtc });
    }

    /// <summary>
    /// Ends the guest session of the request (if any) and moves its cart to the signed-in customer
    /// </summary>
    protected virtual async Task TakeOverGuestSessionAsync(Customer customer)
    {
        var token = CurrentToken;
        if (token is not { Kind: ApiTokenKind.Storefront } || token.CustomerId == customer.Id)
            return;

        var guest = await _customerService.GetCustomerByIdAsync(token.CustomerId);
        if (guest != null && await _customerService.IsGuestAsync(guest))
            await _shoppingCartService.MigrateShoppingCartAsync(guest, customer, true);

        await _apiTokenService.RevokeTokenAsync(token);
    }

    #endregion

    #region Storefront

    /// <summary>
    /// Opens an anonymous session (guest customer) to browse and fill a cart before signing in
    /// </summary>
    [HttpPost("guest")]
    public virtual async Task<IActionResult> Guest()
    {
        if (!_settings.StorefrontEnabled)
            return StorefrontDisabled();

        var customer = await _customerService.InsertGuestCustomerAsync();
        if (!await _permissionService.AuthorizeAsync(RestApiPermissionConfigManager.USE_STOREFRONT_API, customer))
            return ApiError(StatusCodes.Status403Forbidden, "forbidden", "Guests are not allowed to use the API.");

        return ApiOk(await IssueStorefrontTokenAsync(customer));
    }

    /// <summary>
    /// Signs a customer in with the credentials of the web store
    /// </summary>
    [HttpPost("login")]
    public virtual async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!_settings.StorefrontEnabled)
            return StorefrontDisabled();

        if (string.IsNullOrWhiteSpace(request?.UsernameOrEmail) || string.IsNullOrEmpty(request.Password))
            return ApiBadRequest("Username or e-mail and password are required.");

        var login = request.UsernameOrEmail.Trim();
        var result = await _customerRegistrationService.ValidateCustomerAsync(login, request.Password);
        switch (result)
        {
            case CustomerLoginResults.Successful:
                break;
            case CustomerLoginResults.MultiFactorAuthenticationRequired:
                return await MfaChallengeAsync(login);
            case CustomerLoginResults.LockedOut:
                return ApiError(StatusCodes.Status403Forbidden, "locked_out", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut"));
            case CustomerLoginResults.NotActive:
                return ApiError(StatusCodes.Status403Forbidden, "not_active", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive"));
            default:
                //logged on the targeted account when it exists, so that the store owner sees attacks on it
                var target = (_customerSettings.UsernamesEnabled
                    ? await _customerService.GetCustomerByUsernameAsync(login)
                    : await _customerService.GetCustomerByEmailAsync(login)) ?? await _workContext.GetCurrentCustomerAsync();
                await _customerActivityService.InsertActivityAsync(target, RestApiDefaults.ActivityLogSignInFailed, $"REST API: failed sign-in for '{login}' ({result}) from {_webHelper.GetCurrentIpAddress()}");
                return ApiError(StatusCodes.Status401Unauthorized, "invalid_credentials", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));
        }

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsernameAsync(login)
            : await _customerService.GetCustomerByEmailAsync(login);
        if (customer == null)
            return ApiError(StatusCodes.Status401Unauthorized, "invalid_credentials", await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));

        if (!await _permissionService.AuthorizeAsync(RestApiPermissionConfigManager.USE_STOREFRONT_API, customer))
            return ApiError(StatusCodes.Status403Forbidden, "forbidden", "This account is not allowed to use the API.");

        return ApiOk(await CompleteSignInAsync(customer));
    }

    /// <summary>
    /// Second step of a sign-in for accounts with a second factor: pending token from auth/login + code
    /// </summary>
    [HttpPost("mfa")]
    public virtual async Task<IActionResult> Mfa([FromBody] MfaRequest request)
    {
        if (!_settings.StorefrontEnabled)
            return StorefrontDisabled();

        if (string.IsNullOrWhiteSpace(request?.MfaToken) || string.IsNullOrWhiteSpace(request.Code))
            return ApiBadRequest("The pending token and the code are required.");

        var pending = await _apiTokenService.GetValidTokenAsync(request.MfaToken.Trim());
        var store = await _storeContext.GetCurrentStoreAsync();
        var customer = pending is { Kind: ApiTokenKind.MfaPending } && pending.StoreId == store.Id
            ? await _customerService.GetCustomerByIdAsync(pending.CustomerId)
            : null;
        if (customer == null || customer.Deleted || !customer.Active)
            return ApiError(StatusCodes.Status401Unauthorized, "invalid_mfa_token", "The pending token is invalid or expired. Sign in again.");

        var provider = await _mfaService.GetActiveProviderAsync(customer);
        var (valid, attemptsLeft) = _mfaService.Verify(pending.Id, customer, provider, request.Code);
        if (!valid)
        {
            await _customerActivityService.InsertActivityAsync(customer, RestApiDefaults.ActivityLogSignInFailed,
                $"REST API: wrong second-factor code ({attemptsLeft} attempt(s) left)");
            if (attemptsLeft == 0)
            {
                await _apiTokenService.RevokeTokenAsync(pending);
                return ApiError(StatusCodes.Status401Unauthorized, "invalid_mfa_token", "Too many wrong codes. Sign in again.");
            }

            return ApiError(StatusCodes.Status401Unauthorized, "invalid_code", $"Wrong code. {attemptsLeft} attempt(s) left.");
        }

        await _apiTokenService.RevokeTokenAsync(pending);

        //the password step skipped the login bookkeeping because the second factor was awaited
        customer.FailedLoginAttempts = 0;
        customer.CannotLoginUntilDateUtc = null;
        customer.LastLoginDateUtc = DateTime.UtcNow;

        return ApiOk(await CompleteSignInAsync(customer));
    }

    /// <summary>
    /// Registers a customer account (same rules as the web store: registration type, password rules, e-mails)
    /// </summary>
    [HttpPost("register")]
    public virtual async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!_settings.StorefrontEnabled)
            return StorefrontDisabled();

        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return ApiError(StatusCodes.Status403Forbidden, "registration_disabled", await _localizationService.GetResourceAsync("Account.Register.Result.Disabled"));

        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return ApiBadRequest("E-mail and password are required.");

        //register the guest of the current session (keeps its cart), otherwise a new guest
        var token = CurrentToken;
        var customer = token is { Kind: ApiTokenKind.Storefront } ? await _customerService.GetCustomerByIdAsync(token.CustomerId) : null;
        if (customer == null || !await _customerService.IsGuestAsync(customer))
            customer = await _customerService.InsertGuestCustomerAsync();

        var store = await _storeContext.GetCurrentStoreAsync();
        var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
        var registrationRequest = new CustomerRegistrationRequest(customer, request.Email.Trim(),
            _customerSettings.UsernamesEnabled ? request.Username?.Trim() : request.Email.Trim(),
            request.Password, _customerSettings.DefaultPasswordFormat, store.Id, isApproved);
        var result = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);
        if (!result.Success)
            return ApiBadRequest("Registration failed.", result.Errors);

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        await _customerService.UpdateCustomerAsync(customer);

        var language = await _workContext.GetWorkingLanguageAsync();
        if (_customerSettings.NotifyNewCustomerRegistration)
            await _workflowMessageService.SendCustomerRegisteredStoreOwnerNotificationMessageAsync(customer, _localizationSettings.DefaultAdminLanguageId);
        await _eventPublisher.PublishAsync(new CustomerRegisteredEvent(customer));

        switch (_customerSettings.UserRegistrationType)
        {
            case UserRegistrationType.EmailValidation:
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
                await _workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, language.Id);
                if (token != null)
                    await _apiTokenService.RevokeTokenAsync(token);
                return ApiCreated(new RegisterResponse { Status = "email_validation" });

            case UserRegistrationType.AdminApproval:
                if (token != null)
                    await _apiTokenService.RevokeTokenAsync(token);
                return ApiCreated(new RegisterResponse { Status = "admin_approval" });

            default:
                await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, language.Id);
                await _eventPublisher.PublishAsync(new CustomerActivatedEvent(customer));
                if (token != null)
                    await _apiTokenService.RevokeTokenAsync(token);
                return ApiCreated(new RegisterResponse { Status = "standard", Token = await IssueStorefrontTokenAsync(customer) });
        }
    }

    #endregion

    #region Back office

    /// <summary>
    /// Issues a back-office session for an API client (client credentials)
    /// </summary>
    [HttpPost("token")]
    public virtual async Task<IActionResult> ClientToken([FromBody] ClientTokenRequest request)
    {
        if (!_settings.BackOfficeEnabled)
            return ApiError(StatusCodes.Status404NotFound, "api_disabled", "This part of the API is disabled.");

        var client = await _apiTokenService.ValidateClientAsync(request?.ClientId, request?.ClientSecret);
        var customer = client == null ? null : await _customerService.GetCustomerByIdAsync(client.CustomerId);
        if (client == null || customer == null || customer.Deleted || !customer.Active)
        {
            var target = customer ?? await _workContext.GetCurrentCustomerAsync();
            await _customerActivityService.InsertActivityAsync(target, RestApiDefaults.ActivityLogSignInFailed, $"REST API: failed client authentication for '{request?.ClientId}' from {_webHelper.GetCurrentIpAddress()}");
            return ApiError(StatusCodes.Status401Unauthorized, "invalid_client", "Invalid client credentials.");
        }

        var ip = _webHelper.GetCurrentIpAddress();
        if (!ApiTokenService.IsIpAllowed(client, ip))
        {
            await _customerActivityService.InsertActivityAsync(customer, RestApiDefaults.ActivityLogRequestRefused,
                $"REST API: client '{client.Name}' refused from IP address {ip} (not in its allowed addresses)");
            return ApiError(StatusCodes.Status403Forbidden, "ip_not_allowed", "This API client may not connect from this IP address.");
        }

        if (!await _permissionService.AuthorizeAsync(RestApiPermissionConfigManager.USE_BACKOFFICE_API, customer))
            return ApiError(StatusCodes.Status403Forbidden, "forbidden", $"The account of this client lacks the permission '{RestApiPermissionConfigManager.USE_BACKOFFICE_API}'.");

        var store = await _storeContext.GetCurrentStoreAsync();
        var lifetime = TimeSpan.FromMinutes(_settings.BackOfficeTokenLifetimeMinutes > 0 ? _settings.BackOfficeTokenLifetimeMinutes : 60);
        var (token, entity) = await _apiTokenService.IssueTokenAsync(ApiTokenKind.BackOffice, customer.Id, store.Id, lifetime, client.Id);

        return ApiOk(new TokenResponse
        {
            AccessToken = token,
            ExpiresAt = entity.ExpiresOnUtc,
            Customer = await _apiModelFactory.PrepareCustomerInfoAsync(customer)
        });
    }

    #endregion

    #region Common

    /// <summary>
    /// Ends the current session
    /// </summary>
    [HttpPost("logout")]
    public virtual async Task<IActionResult> Logout()
    {
        var token = CurrentToken;
        if (token == null)
            return ApiError(StatusCodes.Status401Unauthorized, "unauthorized", "An access token is required.");

        await _apiTokenService.RevokeTokenAsync(token);

        return NoContent();
    }

    #endregion
}
